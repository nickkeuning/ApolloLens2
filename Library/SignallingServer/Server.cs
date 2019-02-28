using System;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using ApolloLensLibrary.Utilities;
using ApolloLensLibrary.Signalling.Protocol;
using Windows.System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Windows.Storage.Streams;
using System.Threading;
using System.Linq;


namespace ApolloLensLibrary.SignallingServer
{
    public class Server
    {
        private StreamSocketListener Listener { get; set; }
        private string Port { get; }

        private Dictionary<int, Peer> Peers { get; }
        private ConcurrentQueue<PeerMessage> Messages { get; }

        private int nextId = 0;
        private int NextId { get { return nextId++; } }

        private Task MessageDispatchTask;
        private CancellationTokenSource CancellationTokenSource { get; }

        public Server(string port)
        {
            this.Port = port;
            this.Listener = new StreamSocketListener();
            this.Peers = new Dictionary<int, Peer>();
            this.Messages = new ConcurrentQueue<PeerMessage>();
            this.CancellationTokenSource = new CancellationTokenSource();
        }

        public async Task Start(CancellationToken ct)
        {
            this.Listener.ConnectionReceived += Server_ConnectionReceived;
            await this.Listener.BindServiceNameAsync(this.Port);

            var token = this.CancellationTokenSource.Token;
            this.MessageDispatchTask = Task.Run(() => this.DispatchMessagesLoop(token), token);
        }

        public async Task Stop()
        {
            this.CancellationTokenSource.Cancel();
            await this.Listener.CancelIOAsync();
            await Task.Run(() => SpinWait.SpinUntil(() => this.MessageDispatchTask.IsCompleted));
        }

        private async void Server_ConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            var id = this.NextId;
            var peer = new Peer(args.Socket, id);

            peer.MessageReceived += this.Peer_MessageReceived;
            peer.LostPeerConnection += this.Peer_LostPeerConnection;
            await peer.Start(this.CancellationTokenSource.Token);

            this.Peers.Add(id, peer);
        }

        private void Peer_LostPeerConnection(object sender, int e)
        {

        }

        private void Peer_MessageReceived(object sender, PeerMessage message)
        {
            this.Messages.Enqueue(message);
        }

        private async Task DispatchMessagesLoop(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (!this.Messages.IsEmpty)
                {
                    this.Messages.TryDequeue(out var message);
                    var targeIds = this.Peers.Keys.Where(key => key != message.SenderId);

                    foreach (var targetId in targeIds)
                    {
                        await this.Peers[targetId].SendMessage(message.Message);
                    }
                }
            }
        }

        class Peer
        {            
            public int Id { get; }
            public event EventHandler<PeerMessage> MessageReceived;
            public event EventHandler<int> LostPeerConnection;


            private IInputStream InStream { get; }
            private IOutputStream OutStream { get; }

            private Task Listener { get; set; }
            private HeartBeatHandler HeartBeat { get; set; }
            private CancellationTokenSource CancellationTokenSource { get; set; }


            public Peer(StreamSocket socket, int id)
            {
                this.InStream = socket.InputStream;
                this.OutStream = socket.OutputStream;
                this.Id = id;
            }                       

            public async Task Start(CancellationToken serverCt)
            {
                this.CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(serverCt);
                var token = this.CancellationTokenSource.Token;
                this.Listener = Task.Run(() => this.Listen(token), token);

                this.HeartBeat = new HeartBeatHandler(this);
                this.HeartBeat.LostConnection += this.HeartBeat_LostConnection;
                await this.HeartBeat.Start();
            }

            public async Task Stop()
            {
                this.CancellationTokenSource.Cancel();
                await Task.Run(() => SpinWait.SpinUntil(() => this.Listener.IsCompleted));
            }

            public async Task SendMessage(string message)
            {
                await TcpProtocol.SendStringToStreamAsync(this.OutStream, message);
            }

            private async Task Listen(CancellationToken ct)
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var message = await TcpProtocol.ReadStringFromStreamAsync(this.InStream, ct);

                        if (MessageProtocol.IsPingAnswer(message))
                        {
                            this.HeartBeat.SetPingReceived();
                        }
                        else
                        {
                            this.MessageReceived?.Invoke(this, new PeerMessage(this.Id, message));
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                }
            }

            private async void HeartBeat_LostConnection(object sender, EventArgs e)
            {
                await this.Stop();
                this.LostPeerConnection?.Invoke(this, this.Id);
            }


            private async Task SendHeartBeat()
            {
                await MessageProtocol.SendMessageToStreamAsync(this.OutStream, MessageProtocol.MessageType.Ping, null);
            }

            private class HeartBeatHandler
            {
                public event EventHandler LostConnection;

                private ThreadPoolTimer HeartBeatTimer { get; set; }
                private Peer Peer { get; }

                private bool PingReceived { get; set; }
                private int Strikes { get; set; }

                private int Period { get; } = 10;
                private int MaxStrikes { get; } = 3;

                public HeartBeatHandler(Peer peer)
                {
                    this.Peer = peer;
                }

                public void SetPingReceived()
                {
                    this.PingReceived = true;
                }

                public async Task Start()
                {
                    var period = TimeSpan.FromSeconds(Period);
                    await this.Peer.SendHeartBeat();
                    this.HeartBeatTimer = ThreadPoolTimer.CreatePeriodicTimer(async (source) => 
                    {
                        await this.CheckForPing(source);
                    }, period);
                }

                private async Task CheckForPing(ThreadPoolTimer timer)
                {
                    if (!this.PingReceived)
                    {
                        this.Strikes++;
                    }

                    if (this.Strikes == this.MaxStrikes)
                    {
                        timer.Cancel();
                        this.LostConnection?.Invoke(this, null);
                    }
                    else
                    {
                        this.PingReceived = false;
                        await this.Peer.SendHeartBeat();
                    }
                }
            }
        }

        class PeerMessage
        {
            public string Message { get; }
            public int SenderId { get; }
            public PeerMessage(int senderId, string message)
            {
                this.SenderId = senderId;
                this.Message = message;
            }
        }
    }
}
