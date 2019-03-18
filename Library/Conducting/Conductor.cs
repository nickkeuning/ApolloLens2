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
        protected Wrapper Wrapper { get; }

        protected IBaseSignaller Signaller { get; set; }

        protected RTCPeerConnection PeerConnection { get; set; }
        protected List<RTCIceCandidate> IceCandidates { get; } = new List<RTCIceCandidate>();

        protected Conductor()
        {
            this.Wrapper = Wrapper.Instance;
        }

        public async Task Initialize(CoreDispatcher coreDispatcher)
        {
            await this.Wrapper.Initialize(coreDispatcher);
        }

        public async Task SayHi()
        {
            await this.Signaller.SendPlainMessage("Hello, World!");
        }

        public async Task ShutDown()
        {
            await this.Signaller.SendShutdown();
        }

        public void DisconnectFromSignallingServer()
        {
            this.Signaller.DisconnectFromServer();
        }

        protected async Task ConnectToSignallingServer(string address)
        {
            this.Signaller = WebSocketSignaller.CreateSignaller();

            this.Signaller.ReceivedIceCandidate += async (s, candidate) =>
            {
                await this.PeerConnection?.AddIceCandidate(candidate);
            };

            this.Signaller.ReceivedPlainMessage += (s, message) =>
            {
                Logger.Log(message);
            };

            this.Signaller.ReceivedShutdown += async (s, e) =>
            {
                await this.Wrapper.DestroyAllMedia();
                this.PeerConnection = null;
                GC.Collect();
            };

            await this.Signaller.ConnectToServer(address);
        }

        protected RTCPeerConnection CreatePeerConnection()
        {
            var connection = new RTCPeerConnection(
                new RTCConfiguration()
                {
                    BundlePolicy = RTCBundlePolicy.Balanced,
                    IceTransportPolicy = RTCIceTransportPolicy.All
                }
            );

            connection.OnIceCandidate += (Event) =>
            {
                this.IceCandidates.Add(Event.Candidate);
            };

            return connection;
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
    }

    public class Caller : Conductor
    {
        public event EventHandler RemoteStreamAdded;
        private MediaElement RemoteVideo { get; }
        private new ICallerSignaller Signaller { get; set; }

        public Caller(MediaElement remoteVideo)
            : base()
        {
            this.RemoteVideo = remoteVideo;
        }

        public new async Task ConnectToSignallingServer(string address) 
        {
            await base.ConnectToSignallingServer(address);
            this.Signaller = (ICallerSignaller)base.Signaller;
            this.Signaller.ReceivedAnswer += async (s, remoteDescription) =>
            {
                await this.PeerConnection?.SetRemoteDescription(remoteDescription);
                await this.SubmitIceCandidatesAsync();
                Logger.Log("Answer received...");
            };
        }

        public async Task StartPeerConnection()
        {
            this.PeerConnection = this.CreatePeerConnection();
            this.PeerConnection.OnAddStream += async (Event) =>
            {
                var remoteVideoTrack = Event.Stream.GetVideoTracks().FirstOrDefault();
                if (remoteVideoTrack != null)
                {
                    await this.Wrapper.BindRemoteVideo(remoteVideoTrack, this.RemoteVideo);
                }
                Logger.Log("Remote stream added to media element...");
                this.RemoteStreamAdded?.Invoke(this, EventArgs.Empty);
            };

            // Attach local media to new connection
            await this.Wrapper.LoadLocalMedia();
            this.Wrapper.AddLocalMediaToPeerConnection(this.PeerConnection);

            // Perform the caller half of the WebRtc connection handshake
            var localDescription = await this.PeerConnection.CreateOffer();
            await this.PeerConnection.SetLocalDescription(localDescription);
            await this.Signaller.SendOffer(localDescription);
            Logger.Log("Offer sent...");

            // Destroy local media since we're the caller
            await this.Wrapper.DestroyLocalMedia();

            await this.SubmitIceCandidatesAsync();
        }
    }

    public class Callee : Conductor
    {
        private new ICalleeSignaller Signaller { get; set; }

        public IList<Wrapper.CaptureProfile> CaptureProfiles => this.Wrapper.CaptureProfiles;
        public void SetSelectedProfile(Wrapper.CaptureProfile captureProfile)
        {
            this.Wrapper.SetSelectedProfile(captureProfile);
        }

        public Callee() : base() { }

        public new async Task ConnectToSignallingServer(string address)
        {
            await base.ConnectToSignallingServer(address);
            this.Signaller = (ICalleeSignaller)base.Signaller;
            this.Signaller.ReceivedOffer += async (s, offer) =>
            {
                Logger.Log("Received offer...");

                this.PeerConnection = this.CreatePeerConnection();

                await this.Wrapper.LoadLocalMedia();
                this.Wrapper.AddLocalMediaToPeerConnection(this.PeerConnection);

                await this.PeerConnection.SetRemoteDescription(offer);
                var answer = await this.PeerConnection.CreateAnswer();
                await this.PeerConnection.SetLocalDescription(answer);

                await this.Signaller.SendAnswer(answer);
                await this.SubmitIceCandidatesAsync();

                Logger.Log("Sent answer...");
            };
        }
    }
}
