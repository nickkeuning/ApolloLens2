using Org.WebRtc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using ApolloLensLibrary.Utilities;
using ApolloLensLibrary.Signalling;

namespace ApolloLensLibrary.Conducting
{
    public abstract class Conductor
    {
        protected CoreDispatcher CoreDispatcher { get; }
        protected Wrapper Wrapper { get; }

        protected IBaseSignaller Signaller { get; set; }

        protected RTCPeerConnection PeerConnection { get; set; }
        protected List<RTCIceCandidate> IceCandidates { get; } = new List<RTCIceCandidate>();


        protected Conductor(CoreDispatcher coreDispatcher)
        {
            this.CoreDispatcher = coreDispatcher;
            this.Wrapper = Wrapper.Instance;
        }

        public async Task Initialize()
        {
            await this.Wrapper.Initialize(this.CoreDispatcher);
        }

        public async Task ShutDown()
        {
            await this.Signaller.SendShutdown();
        }

        protected async Task ConnectToSignallingServer(string address)
        {
            this.Signaller = WebSocketSignaller.CreateSignaller();
            await this.Signaller.ConnectToServer(address);

            this.Signaller.ReceivedIceCandidate += this.Signaller_ReceivedIceCandidate;
            this.Signaller.ReceivedPlainMessage += this.Signaller_ReceivedPlain;
            this.Signaller.ReceivedShutdown += this.Signaller_ReceivedShutdown;
        }

        private async void Signaller_ReceivedShutdown(object sender, EventArgs e)
        {
            await this.Wrapper.DestroyAllMedia();
        }

        public void DisconnectFromSignallingServer()
        {
            this.Signaller.DisconnectFromServer();
        }

        protected void Signaller_ReceivedPlain(object sender, string e)
        {
            Logger.Log(e);
        }

        protected async void Signaller_ReceivedIceCandidate(object sender, RTCIceCandidate candidate)
        {
            await this.PeerConnection?.AddIceCandidate(candidate);
        }

        protected async Task SubmitIceCandidatesAsync()
        {
            var Complete = RTCIceGatheringState.Complete;
            await Task.Run(() => SpinWait.SpinUntil(() => this.PeerConnection?.IceGatheringState == Complete));
            foreach (var candidate in this.IceCandidates)
            {
                await this.Signaller.SendIceCandidate(candidate);
            }
        }

        public async Task SayHi()
        {
            await this.Signaller.SendPlainMessage("Hello, World!");
        }

        protected void PeerConnection_OnIceCandidate(RTCPeerConnectionIceEvent evt)
        {
            //Logger.Log("On ice candidate triggered");
            this.IceCandidates.Add(evt.Candidate);
        }
    }

    public class Caller : Conductor
    {
        public event EventHandler RemoteStreamAdded;
        private MediaElement RemoteVideo { get; }
        private new ICallerSignaller Signaller { get; set; }

        public Caller(CoreDispatcher coreDispatcher, MediaElement remoteVideo)
            : base(coreDispatcher)
        {
            this.RemoteVideo = remoteVideo;
        }

        public new async Task ConnectToSignallingServer(string address) 
        {
            await base.ConnectToSignallingServer(address);
            this.Signaller = (ICallerSignaller)base.Signaller;
            this.Signaller.ReceivedAnswer += this.Signaller_ReceivedAnswer;
        }


        private async void Signaller_ReceivedAnswer(object sender, RTCSessionDescription remoteDescription)
        {
            await this.PeerConnection?.SetRemoteDescription(remoteDescription);
            await this.SubmitIceCandidatesAsync();
            Logger.Log("Answer received...");
        }


        public async Task StartPeerConnection()
        {
            // Initialize PeerConnection
            this.PeerConnection = new RTCPeerConnection(
                new RTCConfiguration()
                {
                    BundlePolicy = RTCBundlePolicy.Balanced,
                    IceTransportPolicy = RTCIceTransportPolicy.All
                }
            );

            this.PeerConnection.OnIceCandidate += PeerConnection_OnIceCandidate;
            this.PeerConnection.OnAddStream += PeerConnection_OnAddStream;

            // Attach local media to new connection
            await this.Wrapper.LoadLocalMedia();
            this.Wrapper.AddLocalMediaToPeerConnection(this.PeerConnection);
            //this.PeerConnection.AddStream(this.Wrapper.MediaStream);

            // Start the WebRtc connection handshake
            var localDescription = await this.PeerConnection.CreateOffer();
            await this.PeerConnection.SetLocalDescription(localDescription);
            await this.Signaller.SendOffer(localDescription);
            Logger.Log("Offer sent...");
            await this.Wrapper.DestroyAllMedia();
        }

        private async void PeerConnection_OnAddStream(MediaStreamEvent evt)
        {
            var remoteVideoTrack = evt.Stream.GetVideoTracks().FirstOrDefault();
            if (remoteVideoTrack != null)
            {
                await this.Wrapper.BindRemoteVideo(remoteVideoTrack, this.RemoteVideo);
                //this.Wrapper.Media.AddVideoTrackMediaElementPair(remoteVideoTrack, this.RemoteVideo, "Remote");
            }
            Logger.Log("Remote stream added to media element...");
            this.RemoteStreamAdded?.Invoke(this, null);
        }
    }

    public class Callee : Conductor
    {
        private new ICalleeSignaller Signaller { get; set; }

        public Callee(CoreDispatcher coreDispatcher) : base(coreDispatcher) { }

        public new async Task ConnectToSignallingServer(string address)
        {
            await base.ConnectToSignallingServer(address);
            this.Signaller = (ICalleeSignaller)base.Signaller;
            this.Signaller.ReceivedOffer += this.Signaller_ReceivedOffer;
        }

        protected async void Signaller_ReceivedOffer(object sender, RTCSessionDescription offer)
        {
            this.PeerConnection = new RTCPeerConnection(
                new RTCConfiguration()
                {
                    BundlePolicy = RTCBundlePolicy.Balanced,
                    IceTransportPolicy = RTCIceTransportPolicy.All
                }
            );

            await this.Wrapper.LoadLocalMedia();
            this.Wrapper.AddLocalMediaToPeerConnection(this.PeerConnection);
            //this.PeerConnection.AddStream(this.Wrapper.MediaStream);

            await this.PeerConnection.SetRemoteDescription(offer);
            var answer = await this.PeerConnection.CreateAnswer();
            await this.PeerConnection.SetLocalDescription(answer);

            await this.Signaller.SendAnswer(answer);
            await this.SubmitIceCandidatesAsync();
        }
    }
}
