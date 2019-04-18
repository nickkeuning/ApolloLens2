using ApolloLensLibrary.Utilities;
using ApolloLensLibrary.WebRtc;
using Org.WebRtc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using MediaDevice = ApolloLensLibrary.WebRtc.MediaDevice;

namespace WebRtcImplOld
{
    public class Conductor : IConductor
    {
        #region Singleton

        public static Conductor Instance { get; } = new Conductor();

        static Conductor() { }
        private Conductor()
        {
            this.media = Media.CreateMedia();
        }

        #endregion

        #region PrivateProperties

        private CoreDispatcher coreDispatcher { get; set; }
        private IWebRtcSignaller<RTCIceCandidate, RTCSessionDescription> signaller { get; set; }
        private MediaElement remoteVideo { get; set; }
        private MediaElement localVideo { get; set; }

        private MediaOptions mediaOptions { get; set; }

        private Media media { get; set; }
        private MediaStream localMediaStream { get; set; }
        private MediaVideoTrack localVideoTrack { get; set; }
        private MediaVideoTrack remoteVideoTrack { get; set; }

        private RTCPeerConnection peerConnection { get; set; }

        private CaptureProfile selectedProfile { get; set; }
        private MediaDevice selectedDevice { get; set; }

        private List<RTCIceCandidate> iceCandidates { get; } = new List<RTCIceCandidate>();

        #endregion

        #region Interface

        public event EventHandler RemoteStreamAdded;

        public IList<CaptureProfile> CaptureProfiles { get; private set; }
        public IList<MediaDevice> MediaDevices { get; private set; }
        public IUISignaller UISignaller
        {
            get
            {
                return this.signaller;
            }
        }

        public async Task Initialize(ConductorConfig config)
        {
            if (config == null)
                throw new ArgumentException();

            this.coreDispatcher = config.CoreDispatcher ?? throw new ArgumentException();

            if (config.Signaller != null)
            {
                this.signaller = new WebRtcSignaller(
                    config.Signaller ?? throw new ArgumentException());

                this.signaller.ReceivedIceCandidate += this.Signaller_ReceivedIceCandidate;
                this.signaller.ReceivedAnswer += this.Signaller_ReceivedAnswer;
                this.signaller.ReceivedOffer += this.Signaller_ReceivedOffer;
            }

            this.localVideo = config.LocalVideo;
            this.remoteVideo = config.RemoteVideo;

            var allowed = await WebRTC.RequestAccessForMediaCapture();
            if (!allowed)
            {
                throw new Exception("Failed to access media for WebRtc...");
            }
            WebRTC.Initialize(this.coreDispatcher);


            this.MediaDevices = this.getMediaDevices().ToList();
            this.selectedDevice = this.MediaDevices.First();

            this.CaptureProfiles = await this.getCaptureProfiles(this.selectedDevice);
            this.selectedProfile = this.CaptureProfiles.First();
        }

        public void SetMediaOptions(MediaOptions options)
        {
            this.mediaOptions = options;
        }

        public void SetSelectedProfile(CaptureProfile captureProfile)
        {
            this.selectedProfile = captureProfile;
        }

        public void SetSelectedVideoDevice(MediaDevice mediaDevice)
        {
            this.selectedDevice = mediaDevice;
        }

        public async Task StartCall()
        {
            this.peerConnection = await this.createPeerConnection();

            var needPeerConnection =
                this.mediaOptions.SendAudio ||
                this.mediaOptions.ReceiveAudio ||
                this.mediaOptions.SendVideo ||
                this.mediaOptions.ReceiveVideo;

            if (needPeerConnection)
            {
                var localDescription = await this.peerConnection.CreateOffer();
                await this.peerConnection.SetLocalDescription(localDescription);
                await this.signaller.SendOffer(localDescription);
                Logger.Log("Offer sent...");

                await this.SubmitIceCandidatesAsync();
            }

            await this.reconfigureLocalStream();
        }

        public async Task Shutdown()
        {
            if (this.peerConnection != null)
            {
                await this.destroyLocalMedia();

                await this.coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.media.RemoveVideoTrackMediaElementPair(this.remoteVideoTrack);
                });
                this.remoteVideoTrack = null;

                this.peerConnection.Close();
                this.peerConnection = null;

                GC.Collect();
            }
        }

        #endregion

        #region PeerConnection

        private async Task<RTCPeerConnection> createPeerConnection()
        {
            this.setWebRtcDeviceAndProfile();

            var connection = new RTCPeerConnection(
                new RTCConfiguration()
                {
                    BundlePolicy = RTCBundlePolicy.Balanced,
                    IceTransportPolicy = RTCIceTransportPolicy.All
                }
            );

            var loadVideo =
                this.mediaOptions.ReceiveVideo ||
                this.mediaOptions.SendVideo ||
                this.mediaOptions.LocalLoopback;
            var loadAudio =
                this.mediaOptions.ReceiveAudio ||
                this.mediaOptions.SendAudio;

            this.localMediaStream = await this.media.GetUserMedia(
                new RTCMediaStreamConstraints()
                {
                    videoEnabled = loadVideo,
                    audioEnabled = loadAudio
                });
            connection.AddStream(this.localMediaStream);

            this.localVideoTrack = this.localMediaStream.GetVideoTracks().FirstOrDefault();

            if (this.mediaOptions.LocalLoopback)
                await this.RunOnUI(() =>
                {
                    this.media.AddVideoTrackMediaElementPair(
                        this.localVideoTrack,
                        this.localVideo,
                        "Local");
                });

            connection.OnIceCandidate += this.Connection_OnIceCandidate;
            connection.OnAddStream += this.Connection_OnAddStream;

            return connection;
        }

        private async void Connection_OnAddStream(MediaStreamEvent ev)
        {
            this.remoteVideoTrack = ev.Stream.GetVideoTracks().FirstOrDefault();
            if (this.remoteVideoTrack != null)
            {
                if (this.mediaOptions.ReceiveVideo)
                {
                    await this.RunOnUI(() =>
                    {
                        this.media.AddVideoTrackMediaElementPair(
                            this.remoteVideoTrack,
                            this.remoteVideo,
                            "Remote");
                    });
                }
                else
                {
                    await this.destroyRemoteVideo();
                }
            }

            var remoteAudioTrack = ev.Stream.GetAudioTracks().FirstOrDefault();
            if (remoteAudioTrack != null)
            {
                if (!this.mediaOptions.ReceiveAudio)
                {
                    this.destroyRemoteAudio();
                }
            }

            this.RemoteStreamAdded?.Invoke(this, EventArgs.Empty);
        }

        private void Connection_OnIceCandidate(RTCPeerConnectionIceEvent ev)
        {
            this.iceCandidates.Add(ev.Candidate);
        }

        private async Task SubmitIceCandidatesAsync()
        {
            var Complete = RTCIceGatheringState.Complete;
            await Task.Run(() => SpinWait.SpinUntil(() => this.peerConnection?.IceGatheringState == Complete));
            foreach (var candidate in this.iceCandidates)
            {
                await this.signaller.SendIceCandidate(candidate);
            }
        }

        #endregion

        #region SignallerHandlers

        private async void Signaller_ReceivedOffer(object sender, RTCSessionDescription offer)
        {
            Logger.Log("Received offer");
            if (this.peerConnection != null)
                return;

            this.peerConnection = await this.createPeerConnection();

            await this.peerConnection.SetRemoteDescription(offer);

            var answer = await this.peerConnection.CreateAnswer();
            await this.peerConnection.SetLocalDescription(answer);
            await this.signaller.SendAnswer(answer);

            await this.reconfigureLocalStream();

            await this.SubmitIceCandidatesAsync();
        }

        private async void Signaller_ReceivedAnswer(object sender, RTCSessionDescription answer)
        {
            Logger.Log("Received answer");
            await this.peerConnection.SetRemoteDescription(answer);
        }

        private async void Signaller_ReceivedIceCandidate(object sender, RTCIceCandidate candidate)
        {
            await this.peerConnection.AddIceCandidate(candidate);
        }

        #endregion

        #region MediaStream

        private async Task reconfigureLocalStream()
        {
            if (!this.mediaOptions.SendVideo && !this.mediaOptions.LocalLoopback)
            {
                await this.destroyLocalMedia();

                this.localMediaStream = await this.media.GetUserMedia(
                    new RTCMediaStreamConstraints()
                    {
                        videoEnabled = false,
                        audioEnabled = this.mediaOptions.SendAudio
                    });
            }
        }

        private async Task destroyRemoteVideo()
        {
            var stream = this.peerConnection.GetRemoteStreams().FirstOrDefault();
            var track = stream.GetVideoTracks().FirstOrDefault();

            if (stream != null && track != null)
            {
                stream.RemoveTrack(track);
                track.Stop();

                await this.RunOnUI(() => { this.media.RemoveVideoTrackMediaElementPair(track); });

                stream = null;
                GC.Collect();
            }
        }

        private void destroyRemoteAudio()
        {
            var stream = this.peerConnection.GetRemoteStreams().FirstOrDefault();
            var track = stream.GetAudioTracks().FirstOrDefault();
            if (stream != null && track != null)
            {
                stream.RemoveTrack(track);
                track.Stop();
                track = null;
                GC.Collect();
            }
        }

        private async Task destroyLocalMedia()
        {
            if (this.localMediaStream != null)
            {
                foreach (var track in this.localMediaStream.GetVideoTracks())
                {
                    this.localMediaStream.RemoveTrack(track);
                    track.Stop();
                }
                this.localMediaStream = null;
            }

            if (this.localVideoTrack != null)
            {
                await this.coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.media.RemoveVideoTrackMediaElementPair(this.localVideoTrack);
                });
                this.localVideoTrack = null;
            }
            GC.Collect();
        }



        #endregion

        #region CaptureParameters

        private void setWebRtcDeviceAndProfile()
        {
            this.media.SelectVideoDevice(
                new Org.WebRtc.MediaDevice(
                    this.selectedDevice.Id,
                    this.selectedDevice.Name));

            var mrcEnabled = false;
            WebRTC.SetPreferredVideoCaptureFormat(
                    (int)this.selectedProfile.Width,
                    (int)this.selectedProfile.Height,
                    (int)this.selectedProfile.FrameRate,
                    mrcEnabled
                );
        }

        private async Task<IList<CaptureProfile>> getCaptureProfiles(MediaDevice mediaDevice)
        {
            var videoDevice = this.media
                .GetVideoCaptureDevices()
                .Where(dev => dev.Id == mediaDevice.Id && dev.Name == mediaDevice.Name)
                .Single();
            var capabilities = await videoDevice.GetVideoCaptureCapabilities();

            return capabilities
                .Select(cap => new CaptureProfile()
                {
                    Width = cap.Width,
                    Height = cap.Height,
                    FrameRate = cap.FrameRate,
                    MrcEnabled = cap.MrcEnabled
                })
                .OrderBy(cap => cap.Width * cap.Height)
                .ThenBy(cap => cap.FrameRate)
                .ToList();
        }

        private IEnumerable<MediaDevice> getMediaDevices()
        {
            return this.media
                .GetVideoCaptureDevices()
                .Select(dev => new MediaDevice()
                {
                    Name = dev.Name,
                    Id = dev.Id
                });
        }

        #endregion

        #region Utility

        private async Task RunOnUI(Action action)
        {
            await this.coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }

        #endregion
    }
}
