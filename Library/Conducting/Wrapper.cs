using Org.WebRtc;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI.Core;

namespace ApolloLensLibrary.Conducting
{
    public sealed class Wrapper
    {
        private static readonly Wrapper instance = new Wrapper();
        private Media media { get; set; }
        private MediaStream mediaStream { get; set; }

        public static Wrapper Instance { get { return Wrapper.instance; } }
        public Media Media { get { return this.media; } }
        public MediaStream MediaStream { get { return this.mediaStream; } }

        static Wrapper() { }
        private Wrapper()
        {
            this.media = null;
            this.mediaStream = null;
        }

        private RTCMediaStreamConstraints Constraints { get; } = new RTCMediaStreamConstraints()
        {
            videoEnabled = true,
            audioEnabled = false
        };

        public async Task Initialize(CoreDispatcher coreDispatcher)
        {
            if (this.media != null || this.mediaStream != null)
            {
                return;
            }

            var allowed = await WebRTC.RequestAccessForMediaCapture();
            if (!allowed)
            {
                throw new Exception("Failed to access media for WebRtc...");
            }

            WebRTC.Initialize(coreDispatcher);

            this.media = Media.CreateMedia();

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

            this.mediaStream = await this.media.GetUserMedia(this.Constraints);
        }
    }
}
