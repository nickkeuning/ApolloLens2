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
        public static Wrapper Instance { get { return Wrapper.instance; } }
        public Media Media { get { return Instance.media; } }
        public MediaStream MediaStream { get { return Instance.mediaStream; } }

        private static readonly Wrapper instance = new Wrapper();
        private Media media { get; set; }
        private MediaStream mediaStream { get; set; }

        private bool allowed { get; set; }

        private CoreDispatcher coreDispatcher { get; set; }
        private static readonly object dispatcherLock = new object();

        private MediaVideoTrack MediaVideoTrack { get; set; }
        private MediaElement LocalVideo { get; set; }

        static Wrapper() { }
        private Wrapper()
        {
            this.media = Media.CreateMedia();
            this.mediaStream = null;
        }

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

            this.allowed = await WebRTC.RequestAccessForMediaCapture();
            if (!this.allowed)
            {
                throw new Exception("Failed to access media for WebRtc...");
            }
            WebRTC.Initialize(this.coreDispatcher);
        }

        public async Task PrepareUserMediaStream()
        {
            this.mediaStream = await this.media.GetUserMedia(this.Constraints);
        }

        public async Task DestroyUserMediaStream()
        {
            foreach (var track in this.mediaStream.GetTracks())
            {
                this.mediaStream.RemoveTrack(track);
                track.Stop();
            }
            this.mediaStream = null;

            await this.DetachLocalVideo();

            GC.Collect();
        }

        public async Task DetachLocalVideo()
        {
            await this.coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Instance.Media.RemoveVideoTrackMediaElementPair(this.MediaVideoTrack);
            });
            this.MediaVideoTrack = null;
        }

        public async Task BindLocalVideo(MediaElement mediaElement)
        {
            this.LocalVideo = mediaElement;
            this.MediaVideoTrack = this.mediaStream.GetVideoTracks().FirstOrDefault();
            await this.coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.media.AddVideoTrackMediaElementPair(this.MediaVideoTrack, this.LocalVideo, "Local");
            });
        }


        public async Task SetToHighestBitrate()
        {
            var videoDevice = this.media.GetVideoCaptureDevices().First();
            var capabilities = await videoDevice.GetVideoCaptureCapabilities();

            var selectedFormat = capabilities
                .OrderBy(cap => cap.Width * cap.Height * cap.FrameRate)
                .LastOrDefault();

            if (selectedFormat != null)
            {
                WebRTC.SetPreferredVideoCaptureFormat(
                    (int)selectedFormat.Width,
                    (int)selectedFormat.Height,
                    (int)selectedFormat.FrameRate,
                    selectedFormat.MrcEnabled
                );
            }
        }

        public async Task SetToLowestBitrate()
        {
            var videoDevice = this.media.GetVideoCaptureDevices().First();
            var capabilities = await videoDevice.GetVideoCaptureCapabilities();

            var selectedFormat = capabilities
                .OrderBy(cap => cap.Width * cap.Height * cap.FrameRate)
                .FirstOrDefault();

            if (selectedFormat != null)
            {
                WebRTC.SetPreferredVideoCaptureFormat(
                    (int)selectedFormat.Width,
                    (int)selectedFormat.Height,
                    (int)selectedFormat.FrameRate,
                    selectedFormat.MrcEnabled
                );
            }
        }
    }
}
