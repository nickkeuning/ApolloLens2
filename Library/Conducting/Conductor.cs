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
    public abstract class Conductor : IBaseConductor
    {
        #region Properties

        protected MediaWrapper MediaWrapper { get; }
        protected RTCPeerConnection PeerConnection { get; set; }
        protected List<RTCIceCandidate> IceCandidates { get; } = new List<RTCIceCandidate>();
        protected MediaElement RemoteVideo { get; set; }
        public IBaseSignaller Signaller { get; protected set; }

        #endregion

        protected Conductor()
        {
            this.MediaWrapper = MediaWrapper.Instance;
        }

        public async Task Initialize(CoreDispatcher coreDispatcher)
        {
            await this.MediaWrapper.Initialize(coreDispatcher);
        }

        public async Task Shutdown()
        {
            await this.MediaWrapper.DestroyLocalMedia();
            await this.MediaWrapper.DestroyRemoteMedia();
        }

        #region Utility

        protected IBaseSignaller InitializeSignaller(IBaseSignaller signaller)
        {
            signaller.ReceivedIceCandidate += async (s, candidate) =>
            {
                await this.PeerConnection?.AddIceCandidate(candidate);
            };

            return signaller;
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

        #endregion
    }

    public class ClientConductor : Conductor, IClientConductor
    {
        public ClientConductor(IClientSignaller signaller, MediaElement remoteVideo) : base()
        {
            this.RemoteVideo = remoteVideo;

            signaller.ReceivedAnswer += async (s, remoteDescription) =>
            {
                await this.PeerConnection?.SetRemoteDescription(remoteDescription);
                await this.SubmitIceCandidatesAsync();
                Logger.Log("Answer received...");
            };
            this.Signaller = this.InitializeSignaller(signaller);
        }

        public event EventHandler RemoteStreamAdded;

        public async Task ConnectToSource()
        {
            this.PeerConnection = this.CreatePeerConnection();
            this.PeerConnection.OnAddStream += async (Event) =>
            {
                var remoteVideoTrack = Event.Stream.GetVideoTracks().FirstOrDefault();
                if (remoteVideoTrack != null)
                {
                    await this.MediaWrapper.BindRemoteVideo(remoteVideoTrack, this.RemoteVideo);
                    this.RemoteStreamAdded?.Invoke(this, EventArgs.Empty);
                }
            };

            // Attach local media to new connection
            await this.MediaWrapper.LoadLocalMedia();
            this.MediaWrapper.AddLocalMediaToPeerConnection(this.PeerConnection);

            // Perform the caller half of the WebRtc connection handshake
            var localDescription = await this.PeerConnection.CreateOffer();
            await this.PeerConnection.SetLocalDescription(localDescription);
            await (this.Signaller as IClientSignaller).SendOffer(localDescription);
            Logger.Log("Offer sent...");

            await this.SubmitIceCandidatesAsync();

            // Destroy local media since we're the caller
            await this.MediaWrapper.DestroyLocalMedia();
        }
    }

    public class SourceConductor : Conductor, ISourceConductor
    {
        public SourceConductor(ISourceSignaller signaller) : base()
        {
            signaller.ReceivedOffer += async (s, offer) =>
            {
                Logger.Log("Received offer...");

                this.PeerConnection = this.CreatePeerConnection();

                await this.MediaWrapper.LoadLocalMedia();
                this.MediaWrapper.AddLocalMediaToPeerConnection(this.PeerConnection);

                await this.PeerConnection.SetRemoteDescription(offer);
                var answer = await this.PeerConnection.CreateAnswer();
                await this.PeerConnection.SetLocalDescription(answer);

                await (this.Signaller as ISourceSignaller).SendAnswer(answer);
                await this.SubmitIceCandidatesAsync();

                Logger.Log("Sent answer...");
            };
            this.Signaller = this.InitializeSignaller(signaller);
        }
    
        IList<MediaWrapper.CaptureProfile> ISourceConductor.GetCaptureProfiles()
        {
            return this.MediaWrapper.CaptureProfiles;
        }

        public void SetSelectedProfile(MediaWrapper.CaptureProfile captureProfile)
        {
            this.MediaWrapper.SetSelectedProfile(captureProfile);
        }
    }

    public interface IBaseConductor
    {
        IBaseSignaller Signaller { get; }
        Task Initialize(CoreDispatcher coreDispatcher);
        Task Shutdown();
    }

    public interface IClientConductor : IBaseConductor
    {
        event EventHandler RemoteStreamAdded;
        Task ConnectToSource();
    }

    public interface ISourceConductor : IBaseConductor
    {
        IList<MediaWrapper.CaptureProfile> GetCaptureProfiles();
        void SetSelectedProfile(MediaWrapper.CaptureProfile captureProfile);
    }
}
