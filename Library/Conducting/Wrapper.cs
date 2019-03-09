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
    public sealed class Wrapper
    {
        #region Singleton

        public static Wrapper Instance { get { return Wrapper.instance; } }
        private static readonly Wrapper instance = new Wrapper();

        static Wrapper() { }
        private Wrapper()
        {
            this.media = Media.CreateMedia();
        }

        #endregion

        private Media media { get; set; }
        private MediaStream mediaStream { get; set; }

        private static readonly object dispatcherLock = new object();
        private CoreDispatcher coreDispatcher { get; set; }

        private MediaVideoTrack LocalVideoTrack { get; set; }
        private MediaVideoTrack RemoteVideoTrack { get; set; }

        public IList<CaptureProfile> CaptureCapabilities { get; set; }
        private CaptureProfile SelectedCapability { get; set; }

        private RTCMediaStreamConstraints Constraints { get; } = new RTCMediaStreamConstraints()
        {
            videoEnabled = true,
            audioEnabled = false
        };

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

            this.CaptureCapabilities = await this.GetCaptureProfiles();
            this.SelectedCapability = this.CaptureCapabilities.FirstOrDefault();
        }

        public async Task LoadLocalMedia()
        {
            var mrcEnabled = false;
            WebRTC.SetPreferredVideoCaptureFormat(
                    (int)this.SelectedCapability.Width,
                    (int)this.SelectedCapability.Height,
                    (int)this.SelectedCapability.FrameRate,
                    mrcEnabled
                );

            this.mediaStream = await this.media.GetUserMedia(this.Constraints);
            this.LocalVideoTrack = mediaStream.GetVideoTracks().FirstOrDefault();
        }

        public void AddLocalMediaToPeerConnection(RTCPeerConnection peerConnection)
        {
            peerConnection.AddStream(this.mediaStream);
        }

        public async Task BindLocalVideo(MediaElement mediaElement)
        {
            await this.coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.media.AddVideoTrackMediaElementPair(this.LocalVideoTrack, mediaElement, "Local");
            });
        }

        public async Task BindRemoteVideo(MediaVideoTrack videoTrack, MediaElement mediaElement)
        {
            this.RemoteVideoTrack = videoTrack;
            await this.coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.media.AddVideoTrackMediaElementPair(this.RemoteVideoTrack, mediaElement, "Remote");
            });
        }

        public async Task DestroyAllMedia()
        {
            foreach (var track in this.mediaStream.GetTracks())
            {
                this.mediaStream.RemoveTrack(track);
                track.Stop();
            }
            this.mediaStream = null;

            await this.coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.media.RemoveVideoTrackMediaElementPair(this.LocalVideoTrack);
                this.media.RemoveVideoTrackMediaElementPair(this.RemoteVideoTrack);
            });
            this.LocalVideoTrack = null;
            this.RemoteVideoTrack = null;
            GC.Collect();
        }

        public void SetSelectedCapability(CaptureProfile captureCapability)
        {
            this.SelectedCapability = captureCapability;
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
            public uint Width { get; set; }
            public uint Height { get; set; }
            public uint FrameRate { get; set; }

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
    }
}
