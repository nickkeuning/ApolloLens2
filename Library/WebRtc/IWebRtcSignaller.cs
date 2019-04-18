using System;
using System.Threading.Tasks;

namespace ApolloLensLibrary.WebRtc
{
    public interface IWebRtcSignaller<IceCandidate, SessionDescription> : IUISignaller
    {
        event EventHandler<IceCandidate> ReceivedIceCandidate;
        event EventHandler<SessionDescription> ReceivedAnswer;
        event EventHandler<SessionDescription> ReceivedOffer;

        Task SendIceCandidate(IceCandidate iceCandidate);
        Task SendOffer(SessionDescription offer);
        Task SendAnswer(SessionDescription answer);
    }

    public interface IUISignaller
    {
        event EventHandler<string> ReceivedPlain;
        event EventHandler ReceivedShutdown;

        Task SendPlain(string message);
        Task SendShutdown();
    }
}
