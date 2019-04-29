using System;
using System.Threading.Tasks;

namespace ApolloLensLibrary.WebRtc
{
    /// <summary>
    /// Defines the interface needed by the conductor
    /// to communicate with a peer over a signalling
    /// channel.
    /// Generic on types so that Org.WebRtc namespace
    /// need not be referenced.
    /// </summary>
    /// <typeparam name="IceCandidate"></typeparam>
    /// <typeparam name="SessionDescription"></typeparam>
    public interface IWebRtcSignaller<IceCandidate, SessionDescription> : IUISignaller
    {
        event EventHandler<IceCandidate> ReceivedIceCandidate;
        event EventHandler<SessionDescription> ReceivedAnswer;
        event EventHandler<SessionDescription> ReceivedOffer;

        Task SendIceCandidate(IceCandidate iceCandidate);
        Task SendOffer(SessionDescription offer);
        Task SendAnswer(SessionDescription answer);
    }

    /// <summary>
    /// Interface needed for two peer UIs to communicate.
    /// Allows sending plain messages (chat style) for
    /// testing or communication, as well as message
    /// indicating shutdown
    /// </summary>
    public interface IUISignaller
    {
        event EventHandler<string> ReceivedPlain;
        event EventHandler ReceivedShutdown;

        Task SendPlain(string message);
        Task SendShutdown();
    }
}
