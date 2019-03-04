using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.WebRtc;


namespace ApolloLensLibrary.Signalling
{
    public interface ICallerSignaller : IBaseSignaller
    {
        Task SendOffer(RTCSessionDescription offer);
        event EventHandler<RTCSessionDescription> ReceivedAnswer;
    }

    public interface ICalleeSignaller : IBaseSignaller
    {
        Task SendAnswer(RTCSessionDescription answer);
        event EventHandler<RTCSessionDescription> ReceivedOffer;
    }

    public interface IBaseSignaller
    {
        Task ConnectToServer(string address);
        void DisconnectFromServer();

        Task SendIceCandidate(RTCIceCandidate iceCandidate);
        Task SendPlainMessage(string message);

        event EventHandler<RTCIceCandidate> ReceivedIceCandidate;
        event EventHandler<string> ReceivedPlainMessage;
    }
}
