using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.WebRtc;
using ApolloLensLibrary.WebRtc;
using ApolloLensLibrary.Signalling;
using Newtonsoft.Json;

namespace WebRtcImplOld
{
    public class WebRtcSignaller : IWebRtcSignaller<RTCIceCandidate, RTCSessionDescription>
    {
        public event EventHandler<RTCIceCandidate> ReceivedIceCandidate;
        public event EventHandler<RTCSessionDescription> ReceivedAnswer;
        public event EventHandler<RTCSessionDescription> ReceivedOffer;
        public event EventHandler<string> ReceivedPlain;
        public event EventHandler ReceivedShutdown;

        private ProtocolSignaller<WebRtcMessage> Signaller { get; }
        private enum WebRtcMessage
        {
            Offer,
            Answer,
            IceCandidate,
            Plain,
            Shutdown
        };

        public WebRtcSignaller(IBasicSignaller signaller)
        {
            var protocol = new MessageProtocol<WebRtcMessage>();
            this.Signaller = new ProtocolSignaller<WebRtcMessage>(
                signaller,
                protocol);

            this.Signaller.ReceivedMessage += (sender, message) =>
            {
                switch (message.Type)
                {
                    case WebRtcMessage.Offer:
                        var offer = JsonConvert.DeserializeObject<RTCSessionDescription>(message.Contents);
                        this.ReceivedOffer?.Invoke(this, offer);
                        break;

                    case WebRtcMessage.Answer:
                        var answer = JsonConvert.DeserializeObject<RTCSessionDescription>(message.Contents);
                        this.ReceivedAnswer?.Invoke(this, answer);
                        break;

                    case WebRtcMessage.IceCandidate:
                        var candidate = JsonConvert.DeserializeObject<RTCIceCandidate>(message.Contents);
                        this.ReceivedIceCandidate?.Invoke(this, candidate);
                        break;

                    case WebRtcMessage.Plain:
                        this.ReceivedPlain?.Invoke(this, message.Contents);
                        break;

                    case WebRtcMessage.Shutdown:
                        this.ReceivedShutdown(this, EventArgs.Empty);
                        break;
                }
            };
        }

        public async Task SendOffer(RTCSessionDescription offer)
        {
            var message = JsonConvert.SerializeObject(offer);
            await this.Signaller.SendMessage(WebRtcMessage.Offer, message);
        }

        public async Task SendAnswer(RTCSessionDescription answer)
        {
            var message = JsonConvert.SerializeObject(answer);
            await this.Signaller.SendMessage(WebRtcMessage.Answer, message);
        }

        public async Task SendIceCandidate(RTCIceCandidate iceCandidate)
        {
            var message = JsonConvert.SerializeObject(iceCandidate);
            await this.Signaller.SendMessage(WebRtcMessage.IceCandidate, message);
        }

        public async Task SendPlain(string message)
        {
            await this.Signaller.SendMessage(WebRtcMessage.Plain, message);
        }

        public async Task SendShutdown()
        {
            await this.Signaller.SendMessage(WebRtcMessage.Shutdown, "");
        }
    }
}
