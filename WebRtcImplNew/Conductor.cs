using ApolloLensLibrary.WebRtc;
using Org.WebRtc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.UI.Core;
using MediaElement = Windows.UI.Xaml.Controls.MediaElement;

namespace WebRtcImplNew
{
    public class Conductor : IConductor
    {
        #region Singleton

        public static Conductor Instance { get; } = new Conductor();

        static Conductor() { }
        private Conductor() { }

        #endregion

        #region PrivateProperties

        private MediaOptions mediaOptions { get; set; }

        private CaptureProfile selectedProfile { get; set; }
        private VideoDevice selectedDevice { get; set; }

        private IMediaStreamTrack remoteVideoTrack;
        private IMediaStreamTrack localVideoTrack;
        private IMediaStreamTrack remoteAudioTrack;
        private IMediaStreamTrack localAudioTrack;

        private IWebRtcFactory webRtcFactory { get; set; }

        private RTCPeerConnection peerConnection { get; set; }

        private CoreDispatcher coreDispatcher { get; set; }
        private IWebRtcSignaller<RTCIceCandidate, RTCSessionDescription> signaller { get; set; }

        private MediaElement remoteVideo { get; set; }
        private MediaElement localVideo { get; set; }

        #endregion

        #region Interface

        public async Task<IList<CaptureProfile>> GetCaptureProfiles(VideoDevice device)
        {
            var mediaCapture = new MediaCapture();
            var mediaSettings = new MediaCaptureInitializationSettings()
            {
                VideoDeviceId = device.Id
            };

            return await mediaCapture
                .InitializeAsync(mediaSettings)
                .AsTask()
                .ContinueWith(initResult =>
                {
                    if (initResult.Exception != null)
                        return null;

                    return mediaCapture
                        .VideoDeviceController
                        .GetAvailableMediaStreamProperties(MediaStreamType.VideoRecord)
                        .Cast<VideoEncodingProperties>()
                        .Select(prop => new CaptureProfile()
                        {
                            Width = prop.Width,
                            Height = prop.Height,
                            FrameRate = prop.FrameRate.Numerator / prop.FrameRate.Denominator,
                            MrcEnabled = true
                        })
                        .GroupBy(profile => profile.ToString())
                        .Select(grp => grp.First())
                        .OrderBy(profile => profile.Width * profile.Height)
                        .ThenBy(profile => profile.FrameRate)
                        .ToList();
                });
        }

        public async Task<IList<VideoDevice>> GetVideoDevices()
        {
            var devices = await VideoCapturer.GetDevices();
            return devices
                .Select(dev => new VideoDevice()
                {
                    Id = dev.Info.Id,
                    Name = dev.Info.Name
                })
                .ToList();
        }

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
                throw new ArgumentException("Config cannon be null");

            this.coreDispatcher =
                config.CoreDispatcher ?? throw new ArgumentException(
                    "Core dispatcher cannot be null");

            if (config.Signaller != null)
            {
                this.signaller = new WebRtcSignaller(config.Signaller);

                this.signaller.ReceivedIceCandidate += this.signaller_ReceivedIceCandidate;
                this.signaller.ReceivedAnswer += this.signaller_ReceivedAnswer;
                this.signaller.ReceivedOffer += this.signaller_ReceivedOffer;
            }

            this.localVideo = config.LocalVideo;
            this.remoteVideo = config.RemoteVideo;

            var allowed = await this.requestAccessForMediaCapture();
            if (!allowed)
                throw new UnauthorizedAccessException("Can't access media");

            await Task.Run(() =>
            {
                var configuration = new WebRtcLibConfiguration();
                var queue = EventQueueMaker.Bind(this.coreDispatcher);
                configuration.Queue = queue;
                configuration.AudioCaptureFrameProcessingQueue = EventQueue.GetOrCreateThreadQueueByName("AudioCaptureProcessingQueue");
                configuration.AudioRenderFrameProcessingQueue = EventQueue.GetOrCreateThreadQueueByName("AudioRenderProcessingQueue");
                configuration.VideoFrameProcessingQueue = EventQueue.GetOrCreateThreadQueueByName("VideoFrameProcessingQueue");
                WebRtcLib.Setup(configuration);
            });
        }

        public void SetMediaOptions(MediaOptions options)
        {
            if (options.LocalLoopback && this.localVideo == null)
                throw new ArgumentException();

            if (options.ReceiveVideo && this.remoteVideo == null)
                throw new ArgumentException();

            this.mediaOptions = options;
        }

        public void SetSelectedProfile(CaptureProfile captureProfile)
        {
            this.selectedProfile = captureProfile;
        }

        public void SetSelectedMediaDevice(VideoDevice mediaDevice)
        {
            this.selectedDevice = mediaDevice;
        }

        public async Task StartCall()
        {
            if (this.peerConnection != null)
                throw new Exception();

            this.peerConnection = await this.buildPeerConnection(this.mediaOptions);

            var connectToPeer =
                this.mediaOptions.SendAudio ||
                this.mediaOptions.ReceiveAudio ||
                this.mediaOptions.SendVideo ||
                this.mediaOptions.ReceiveVideo;

            if (connectToPeer)
            {
                var offerOptions = new RTCOfferOptions()
                {
                    OfferToReceiveAudio = this.mediaOptions.ReceiveAudio,
                    OfferToReceiveVideo = this.mediaOptions.ReceiveVideo
                };

                var offer = await this.peerConnection.CreateOffer(offerOptions);
                await this.peerConnection.SetLocalDescription(offer);
                await this.signaller.SendOffer((RTCSessionDescription)offer);
            }
        }

        public Task Shutdown()
        {
            if (this.peerConnection != null)
            {
                this.peerConnection.OnIceCandidate -= this.peerConnection_OnIceCandidate;
                this.peerConnection.OnTrack -= this.peerConnection_OnTrack;

                if (this.remoteVideoTrack != null) this.remoteVideoTrack.Element = null;
                if (this.localVideoTrack != null) this.localVideoTrack.Element = null;

                (this.remoteVideoTrack as IDisposable)?.Dispose();
                (this.localVideoTrack as IDisposable)?.Dispose();
                (this.remoteAudioTrack as IDisposable)?.Dispose();
                (this.localAudioTrack as IDisposable)?.Dispose();

                this.remoteVideoTrack = null;
                this.localVideoTrack = null;
                this.remoteAudioTrack = null;
                this.localAudioTrack = null;

                (this.peerConnection as IDisposable)?.Dispose();
                this.peerConnection = null;

                GC.Collect();
            }

            return Task.CompletedTask;
        }

        #endregion

        #region PeerConnection

        private async Task<RTCPeerConnection> buildPeerConnection(MediaOptions mediaOptions)
        {
            return await Task.Run(() =>
            {
                var factory = new WebRtcFactory(new WebRtcFactoryConfiguration());

                var peerConnection = new RTCPeerConnection(
                    new RTCConfiguration()
                    {
                        Factory = factory,
                        BundlePolicy = RTCBundlePolicy.Balanced,
                        IceTransportPolicy = RTCIceTransportPolicy.All
                    });

                peerConnection.OnTrack += this.peerConnection_OnTrack;

                if (mediaOptions.SendVideo || mediaOptions.LocalLoopback)
                {
                    this.localVideoTrack = this.getLocalVideo(factory);
                }

                if (mediaOptions.SendAudio)
                {
                    this.localAudioTrack = this.getLocalAudio(factory);
                }

                if (mediaOptions.SendVideo)
                {
                    peerConnection.AddTrack(this.localVideoTrack);
                }
                if (mediaOptions.SendAudio)
                {
                    peerConnection.AddTrack(this.localAudioTrack);
                }

                if (mediaOptions.LocalLoopback)
                {
                    this.localVideoTrack.Element = MediaElementMaker.Bind(this.localVideo);
                }

                return peerConnection;
            });
        }

        private void peerConnection_OnTrack(IRTCTrackEvent ev)
        {
            if (ev.Track.Kind == "video")
            {
                this.remoteVideoTrack = ev.Track;
                if (this.mediaOptions.ReceiveVideo)
                {
                    this.remoteVideoTrack.Element = MediaElementMaker.Bind(this.remoteVideo);
                }
            }
            else if (ev.Track.Kind == "audio")
            {
                if (this.mediaOptions.ReceiveAudio)
                {
                    this.remoteAudioTrack = ev.Track;
                }
            }

        }

        private async void peerConnection_OnIceCandidate(IRTCPeerConnectionIceEvent ev)
        {
            await this.signaller.SendIceCandidate((RTCIceCandidate)ev.Candidate);
        }

        private IMediaStreamTrack getLocalVideo(IWebRtcFactory factory)
        {
            IReadOnlyList<IConstraint> mandatoryConstraints = new List<IConstraint>() {
                new Constraint("maxWidth", this.selectedProfile.Width.ToString()),
                new Constraint("minWidth", this.selectedProfile.Width.ToString()),
                new Constraint("maxHeight", this.selectedProfile.Height.ToString()),
                new Constraint("minHeight", this.selectedProfile.Height.ToString()),
                new Constraint("maxFrameRate", this.selectedProfile.FrameRate.ToString()),
                new Constraint("minFrameRate", this.selectedProfile.FrameRate.ToString())
            };
            IReadOnlyList<IConstraint> optionalConstraints = new List<IConstraint>();
            var mediaConstraints = new MediaConstraints(mandatoryConstraints, optionalConstraints);

            var videoCapturer = VideoCapturer.Create(this.selectedDevice.Name, this.selectedDevice.Id, false);

            VideoOptions options = new VideoOptions()
            {
                Factory = factory,
                Capturer = videoCapturer,
                Constraints = mediaConstraints
            };
            var videoTrackSource = VideoTrackSource.Create(options);
            return MediaStreamTrack.CreateVideoTrack("LocalVideo", videoTrackSource);
        }

        private IMediaStreamTrack getLocalAudio(IWebRtcFactory factory)
        {
            AudioOptions audioOptions = new AudioOptions()
            {
                Factory = factory
            };
            var audioTrackSource = AudioTrackSource.Create(audioOptions);
            return MediaStreamTrack.CreateAudioTrack("LocalAudio", audioTrackSource);
        }

        #endregion

        #region SignallerHandlers

        private async void signaller_ReceivedIceCandidate(object sender, RTCIceCandidate candidate)
        {
            await this.peerConnection.AddIceCandidate(candidate);
        }

        private async void signaller_ReceivedOffer(object sender, RTCSessionDescription offer)
        {
            if (this.peerConnection != null)
                return;

            this.peerConnection = await this.buildPeerConnection(this.mediaOptions);

            await this.peerConnection.SetRemoteDescription(offer);

            var answer = await this.peerConnection.CreateAnswer(new RTCAnswerOptions());
            await this.peerConnection.SetLocalDescription(answer);
            await this.signaller.SendAnswer((RTCSessionDescription)answer);

            this.peerConnection.OnIceCandidate += this.peerConnection_OnIceCandidate;
        }

        private async void signaller_ReceivedAnswer(object sender, RTCSessionDescription answer)
        {
            await this.peerConnection.SetRemoteDescription(answer);
            this.peerConnection.OnIceCandidate += this.peerConnection_OnIceCandidate;
        }

        #endregion

        #region MediaInitialization

        private async Task<bool> requestAccessForMediaCapture()
        {
            var requester = new MediaCapture();
            var mediaSettings = new MediaCaptureInitializationSettings()
            {
                AudioDeviceId = "",
                VideoDeviceId = "",
                StreamingCaptureMode = StreamingCaptureMode.AudioAndVideo,
                PhotoCaptureSource = PhotoCaptureSource.VideoPreview
            };

            return await requester
                .InitializeAsync(mediaSettings)
                .AsTask()
                .ContinueWith(initResult =>
                {
                    return initResult.Exception == null;
                });
        }

        #endregion
    }
}
