using Org.WebRtc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using System.Collections.Generic;
using ApolloLensLibrary.Utilities;

namespace ApolloLensLibrary.Conducting
{
    public sealed class MediaWrapper
    {
        #region Singleton

        public static MediaWrapper Instance { get { return MediaWrapper.instance; } }
        private static readonly MediaWrapper instance = new MediaWrapper();

        static MediaWrapper() { }
        private MediaWrapper()
        {
            this.media = Media.CreateMedia();
        }

        public async Task Initialize(CoreDispatcher coreDispatcher)
        {
            lock (dispatcherLock)
            {
                if (this.coreDispatcher == null)
                {
                    this.coreDispatcher = coreDispatcher;
                }
            }

            var allowed = await WebRTC.RequestAccessForMediaCapture();
            if (!allowed)
            {
                throw new Exception("Failed to access media for WebRtc...");
            }
            WebRTC.Initialize(this.coreDispatcher);

            this.CaptureProfiles = await this.GetCaptureProfiles();
            this.SelectedProfile = this.CaptureProfiles.FirstOrDefault();
        }


        #endregion

        #region Properties

        private Media media { get; set; }
        private MediaStream localMediaStream { get; set; }

        private static readonly object dispatcherLock = new object();
        private CoreDispatcher coreDispatcher { get; set; }

        private MediaVideoTrack LocalVideoTrack { get; set; }
        private MediaVideoTrack RemoteVideoTrack { get; set; }

        private static readonly RTCMediaStreamConstraints Constraints = new RTCMediaStreamConstraints()
        {
            videoEnabled = true,
            audioEnabled = false
        };

        #endregion

        #region RemoteMedia

        public async Task BindRemoteVideo(MediaVideoTrack videoTrack, MediaElement mediaElement)
        {
            this.RemoteVideoTrack = videoTrack;
            await this.coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.media.AddVideoTrackMediaElementPair(this.RemoteVideoTrack, mediaElement, "Remote");
            });
        }

        public async Task DestroyRemoteMedia()
        {
            if (this.RemoteVideoTrack != null)
            {
                await this.coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.media.RemoveVideoTrackMediaElementPair(this.RemoteVideoTrack);
                });
                this.RemoteVideoTrack = null;
            }
        }

        #endregion

        #region LocalMedia

        public async Task LoadLocalMedia()
        {
            var mrcEnabled = false;
            WebRTC.SetPreferredVideoCaptureFormat(
                    (int)this.SelectedProfile.Width,
                    (int)this.SelectedProfile.Height,
                    (int)this.SelectedProfile.FrameRate,
                    mrcEnabled
                );

            this.localMediaStream = await this.media.GetUserMedia(MediaWrapper.Constraints);
            this.LocalVideoTrack = localMediaStream.GetVideoTracks().FirstOrDefault();
        }

        public void AddLocalMediaToPeerConnection(RTCPeerConnection peerConnection)
        {
            peerConnection.AddStream(this.localMediaStream);
        }

        public async Task BindLocalVideo(MediaElement mediaElement)
        {
            await this.coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.media.AddVideoTrackMediaElementPair(this.LocalVideoTrack, mediaElement, "Local");
            });
        }

        public async Task DestroyLocalMedia()
        {
            if (this.localMediaStream != null)
            {
                foreach (var track in this.localMediaStream.GetTracks())
                {
                    this.localMediaStream.RemoveTrack(track);
                    track.Stop();
                }
                this.localMediaStream = null;
            }

            if (this.LocalVideoTrack != null)
            {
                await this.coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.media.RemoveVideoTrackMediaElementPair(this.LocalVideoTrack);
                });
                this.LocalVideoTrack = null;
            }
        }

        #endregion

        #region CaptureProfiles

        public IList<CaptureProfile> CaptureProfiles { get; private set; }
        private CaptureProfile SelectedProfile { get; set; }

        public void SetSelectedProfile(CaptureProfile captureCapability)
        {
            this.SelectedProfile = captureCapability;
        }

        private async Task<IList<CaptureProfile>> GetCaptureProfiles()
        {
            var videoDevice = this.media.GetVideoCaptureDevices().First();
            var capabilities = await videoDevice.GetVideoCaptureCapabilities();

            return capabilities
                .Select(cap => new CaptureProfile(cap))
                .OrderBy(cap => cap.Width * cap.Height)
                .ThenBy(cap => cap.FrameRate)
                .ToList();
        }

        public class CaptureProfile
        {
            public uint Width { get; }
            public uint Height { get; }
            public uint FrameRate { get; }

            public CaptureProfile(CaptureCapability captureCapability)
            {
                this.Width = captureCapability.Width;
                this.Height = captureCapability.Height;
                this.FrameRate = captureCapability.FrameRate;
            }

            public override string ToString()
            {
                return $"{Width} x {Height} {FrameRate} fps";
            }
        }

        #endregion
    }
}
