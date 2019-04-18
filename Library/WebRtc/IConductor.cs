using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using ApolloLensLibrary.Signalling;

namespace ApolloLensLibrary.WebRtc
{
    public interface IConductor
    {
        event EventHandler RemoteStreamAdded;

        IUISignaller UISignaller { get; }

        IList<CaptureProfile> CaptureProfiles { get; }
        IList<MediaDevice> MediaDevices { get; }

        void SetSelectedProfile(CaptureProfile captureProfile);
        void SetSelectedVideoDevice(MediaDevice mediaDevice);
        void SetMediaOptions(MediaOptions options);

        Task Initialize(ConductorConfig config);
        Task StartCall();
        Task Shutdown();
    }

    public struct MediaOptions
    {
        /// <summary>
        /// Allows media options to be an immutable struct
        /// but still be initialized with an object initializer
        /// </summary>
        public struct Init
        {
            public bool SendVideo { get; set; }
            public bool SendAudio { get; set; }

            public bool ReceiveVideo { get; set; }
            public bool ReceiveAudio { get; set; }

            public bool LocalLoopback { get; set; }
        }

        public MediaOptions(Init init)
        {
            this.SendAudio = init.SendAudio;
            this.SendVideo = init.SendVideo;
            this.ReceiveAudio = init.ReceiveAudio;
            this.ReceiveVideo = init.ReceiveVideo;
            this.LocalLoopback = init.LocalLoopback;
        }

        public bool SendVideo { get; }
        public bool SendAudio { get; }

        public bool ReceiveVideo { get; }
        public bool ReceiveAudio { get; }

        public bool LocalLoopback { get; }
    }

    public class ConductorConfig
    {
        public MediaElement RemoteVideo { get; set; }
        public MediaElement LocalVideo { get; set; }
        public CoreDispatcher CoreDispatcher { get; set; }
        public IBasicSignaller Signaller { get; set; }
    }


    public class MediaDevice
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }

    public class CaptureProfile
    {
        public uint Width { get; set; }
        public uint Height { get; set; }
        public uint FrameRate { get; set; }
        public bool MrcEnabled { get; set; }

        public override string ToString()
        {
            return $"{this.Width} x {this.Height} {this.FrameRate} fps";
        }
    }
}
