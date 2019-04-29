using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using ApolloLensLibrary.Signalling;


/// <summary>
/// Defines an interface capable of accessing local
/// media and conducting a configurable two way
/// video / audio call between devices.
/// Implemented using WebRtc nuget package in other
/// assemblies.
/// </summary>
namespace ApolloLensLibrary.WebRtc
{
    /// <summary>
    /// Interface to wrap up the Org.WebRtc namespace.
    /// Meant to be easier to use / configure than 
    /// WebRtc for UWP example conductor.
    /// </summary>
    public interface IConductor
    {
        /// <summary>
        /// Exposes internal signaller object through the
        /// IUISignaller interface.
        /// </summary>
        IUISignaller UISignaller { get; }

        /// <summary>
        /// Returns all available capture profiles for the
        /// specified device
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        Task<IList<CaptureProfile>> GetCaptureProfiles(VideoDevice device);

        /// <summary>
        /// Returns all available video capture devices.
        /// </summary>
        /// <returns></returns>
        Task<IList<VideoDevice>> GetVideoDevices();

        /// <summary>
        /// Set the desired video capture device.
        /// I.e., webcam, capture card, usb webcam
        /// </summary>
        /// <param name="mediaDevice"></param>
        void SetSelectedMediaDevice(VideoDevice mediaDevice);

        /// <summary>
        /// Set the desired capture profile.
        /// </summary>
        /// <param name="captureProfile"></param>
        void SetSelectedProfile(CaptureProfile captureProfile);

        /// <summary>
        /// Set the desired media options.
        /// Will throw an exception if the conductor
        /// is not configured to support these options.
        /// </summary>
        /// <param name="options"></param>
        void SetMediaOptions(MediaOptions options);

        /// <summary>
        /// Asynchronously initialize the object.
        /// IConductor should be implemented as a
        /// singleton, and needs to be initialized,
        /// since creation occurs statically.
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        Task Initialize(ConductorConfig config);

        /// <summary>
        /// Start a "call" based on the current
        /// media options
        /// </summary>
        /// <returns></returns>
        Task StartCall();

        /// <summary>
        /// Shut down a running call, release most 
        /// resources.
        /// </summary>
        /// <returns></returns>
        Task Shutdown();
    }

    /// <summary>
    /// Immutable struct, used to specify which media to 
    /// load and what to do with it in an IConductor
    /// instance
    /// </summary>
    public struct MediaOptions
    {
        /// <summary>
        /// Internal mutable struct containing the
        /// same fields.
        /// Allows media options to be immutable, but
        /// still be initialized with an object initializer.
        /// </summary>
        public struct Init
        {
            public bool SendVideo { get; set; }
            public bool SendAudio { get; set; }

            public bool ReceiveVideo { get; set; }
            public bool ReceiveAudio { get; set; }

            public bool LocalLoopback { get; set; }
        }

        /// <summary>
        /// Constructor. Takes in mutable MediaOptions.Init 
        /// object.
        /// </summary>
        /// <example>
        /// var options = new MediaOptions(
        ///     new MediaOptions.Init()
        ///        {
        ///            ReceiveVideo = true
        ///        });
        /// </example>
        /// <param name="init"></param>
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

    /// <summary>
    /// Configuration object for initializing an
    /// IConductor. Contains dependencies for
    /// injection. Core dispatcher is always
    /// required. The others may be left null
    /// depending on the IConductor's media options
    /// </summary>
    public class ConductorConfig
    {
        /// <summary>
        /// The UI.Xaml element to render remote video to
        /// </summary>
        public MediaElement RemoteVideo { get; set; }

        /// <summary>
        /// The UI.Xaml element to render local video to
        /// </summary>
        public MediaElement LocalVideo { get; set; }

        /// <summary>
        /// The UI core dispatcher.
        /// </summary>
        public CoreDispatcher CoreDispatcher { get; set; }

        /// <summary>
        /// An implementation of the IBasic signaller,
        /// capable of sending and receiving messages.
        /// </summary>
        public IBasicSignaller Signaller { get; set; }
    }

    /// <summary>
    /// Represents a video device.
    /// </summary>
    public class VideoDevice
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            return this.Name;
        }
    }

    /// <summary>
    /// Represents a capture profile.
    /// </summary>
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
