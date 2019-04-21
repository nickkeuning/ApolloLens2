using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Org.WebRtc;
using ApolloLensLibrary.Signalling;
using ApolloLensLibrary.WebRtc;
using Newtonsoft.Json;

namespace WebRtcImplNew
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
                        var offer = this.DeserializeSessionDescription(message.Contents);
                        this.ReceivedOffer?.Invoke(this, offer);
                        break;

                    case WebRtcMessage.Answer:
                        var answer = this.DeserializeSessionDescription(message.Contents);
                        this.ReceivedAnswer?.Invoke(this, answer);
                        break;

                    case WebRtcMessage.IceCandidate:
                        var init = JsonConvert.DeserializeObject<RTCIceCandidateInit>(message.Contents);
                        var candidate = new RTCIceCandidate(init);
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
            var message = this.SerializeSessionDescription(offer);
            await this.Signaller.SendMessage(WebRtcMessage.Offer, message);
        }

        public async Task SendAnswer(RTCSessionDescription answer)
        {
            var message = this.SerializeSessionDescription(answer);
            await this.Signaller.SendMessage(WebRtcMessage.Answer, message);
        }

        public async Task SendIceCandidate(RTCIceCandidate iceCandidate)
        {
            var init = iceCandidate.ToJson();
            var message = JsonConvert.SerializeObject(init);
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

        private string SerializeSessionDescription(RTCSessionDescription description)
        {
            var init = new RTCSessionDescriptionInit()
            {
                Sdp = description.Sdp,
                Type = description.SdpType
            };

            return JsonConvert.SerializeObject(init);
        }

        private RTCSessionDescription DeserializeSessionDescription(string raw)
        {
            var init = JsonConvert.DeserializeObject<RTCSessionDescriptionInit>(raw);
            return new RTCSessionDescription(init);
        }
    }
}
