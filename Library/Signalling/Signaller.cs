using ApolloLensLibrary.Signalling.Protocol;
using ApolloLensLibrary.Utilities;
using Newtonsoft.Json;
using Org.WebRtc;
using System;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Networking;
using Windows.Networking.Sockets;
using System.Threading;
using Windows.Storage.Streams;
using MessageType = ApolloLensLibrary.Signalling.Protocol.MessageProtocol.MessageType;

namespace ApolloLensLibrary.Signalling
{
    public class Signaller : ICalleeSignaller, ICallerSignaller
    {


        public event EventHandler<RTCSessionDescription> ReceivedOffer;
        public event EventHandler<RTCSessionDescription> ReceivedAnswer;
        public event EventHandler<RTCIceCandidate> ReceivedIceCandidate;
        public event EventHandler<string> ReceivedPlainMessage;

        private StreamSocket Socket { get; set; }
        private string ServerAddress { get; }
        private string Port { get; }

        private Task PollTask;
        private CancellationTokenSource CancellationTokenSource { get; }

        private Signaller(string serverAddress, string serverPort)
        {
            this.Port = serverPort;
            this.ServerAddress = serverAddress;
            this.Socket = new StreamSocket();
            this.CancellationTokenSource = new CancellationTokenSource();
        }

        public static IBaseSignaller CreateSignaller(string serverAddress, string serverPort)
        {
            return new Signaller(serverAddress, serverPort);
        }

        public async Task ConnectToSignallingServer()
        {
            var hostname = new HostName(this.ServerAddress);
            await this.Socket.ConnectAsync(hostname, this.Port);
            this.PollTask = Task.Run(() => this.LongPoll());
        }

        public async Task SendPlainMessage(string message)
        {
            await MessageProtocol.SendMessageToStreamAsync(this.Socket.OutputStream, MessageType.Plain, message);
        }

        public async Task SendOffer(RTCSessionDescription offer)
        {
            var messageContents = JsonConvert.SerializeObject(offer);
            await MessageProtocol.SendMessageToStreamAsync(this.Socket.OutputStream, MessageType.Offer, messageContents);
        }

        public async Task SendAnswer(RTCSessionDescription answer)
        {
            var messageContents = JsonConvert.SerializeObject(answer);
            await MessageProtocol.SendMessageToStreamAsync(this.Socket.OutputStream, MessageType.Answer, messageContents);
        }

        public async Task SendIceCandidate(RTCIceCandidate iceCandidate)
        {
            var messageContents = JsonConvert.SerializeObject(iceCandidate);
            await MessageProtocol.SendMessageToStreamAsync(this.Socket.OutputStream, MessageType.IceCandidate, messageContents);
        }

        private async Task LongPoll()
        {
            while (true)
            {
                var message = await MessageProtocol.ReadMessageFromStreamAsync(this.Socket.InputStream, this.CancellationTokenSource.Token);
                switch (message.Type)
                {
                    case MessageType.Offer:
                        {
                            var offer = JsonConvert.DeserializeObject<RTCSessionDescription>(message.Contents);
                            this.ReceivedOffer?.Invoke(this, offer);
                            break;
                        }
                    case MessageType.Answer:
                        {
                            var answer = JsonConvert.DeserializeObject<RTCSessionDescription>(message.Contents);
                            this.ReceivedAnswer?.Invoke(this, answer);
                            break;
                        }
                    case MessageType.IceCandidate:
                        {
                            var iceCandidate = JsonConvert.DeserializeObject<RTCIceCandidate>(message.Contents);
                            this.ReceivedIceCandidate?.Invoke(this, iceCandidate);
                            break;
                        }
                    case MessageType.Plain:
                        {
                            this.ReceivedPlainMessage?.Invoke(this, message.Contents);
                            break;
                        }
                    case MessageType.PingAnswer:
                        {
                            await MessageProtocol.SendMessageToStreamAsync(this.Socket.OutputStream, MessageType.PingAnswer, null);
                            break;
                        }
                    default:
                        {
                            throw new Exception("Server received wrong message type.");
                        }
                }
            }
        }


    }

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
        Task ConnectToSignallingServer();

        Task SendIceCandidate(RTCIceCandidate iceCandidate);
        Task SendPlainMessage(string message);

        event EventHandler<RTCIceCandidate> ReceivedIceCandidate;
        event EventHandler<string> ReceivedPlainMessage;
    }
}
