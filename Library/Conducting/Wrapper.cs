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
        }

        public async Task LoadLocalMedia()
        {
            this.mediaStream = await this.media.GetUserMedia(this.Constraints);
            this.LocalVideoTrack = mediaStream.GetVideoTracks().FirstOrDefault();
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

        public void AddLocalMediaToPeerConnection(RTCPeerConnection peerConnection)
        {
            peerConnection.AddStream(this.mediaStream);
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
