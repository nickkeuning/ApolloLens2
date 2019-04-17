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

        public ISignaller Signaller { get; protected set; }

        #endregion

        protected Conductor(ISignaller signaller)
        {
            this.Signaller = this.InitializeSignaller(signaller);
            this.MediaWrapper = MediaWrapper.Instance;
        }

        private ISignaller InitializeSignaller(ISignaller signaller)
        {
            signaller.ReceivedIceCandidate += async (s, candidate) =>
            {
                await this.PeerConnection?.AddIceCandidate(candidate);
            };
            return signaller;
        }

        public async Task Initialize(CoreDispatcher coreDispatcher)
        {
            await this.MediaWrapper.Initialize(coreDispatcher);
        }

        public async Task Shutdown()
        {
            await this.MediaWrapper.DestroyLocalMedia();
            await this.MediaWrapper.DestroyRemoteMedia();
            if (this.PeerConnection != null)
            {
                this.PeerConnection.Close();
                this.PeerConnection = null;
            }
            GC.Collect();
        }

        #region Utility

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

    public class CallerConductor : Conductor, ICallerConductor
    {
        public CallerConductor(ISignaller signaller, MediaElement remoteVideo) 
            : base(signaller)
        {
            this.RemoteVideo = remoteVideo;

            this.Signaller.ReceivedAnswer += async (s, remoteDescription) =>
            {
                await this.PeerConnection?.SetRemoteDescription(remoteDescription);
                await this.SubmitIceCandidatesAsync();
                Logger.Log("Answer received...");
            };
        }

        public event EventHandler RemoteStreamAdded;

        public async Task StartCall()
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
            await this.Signaller.SendOffer(localDescription);
            Logger.Log("Offer sent...");

            await this.SubmitIceCandidatesAsync();

            // Destroy local media since we're the caller
            await this.MediaWrapper.DestroyLocalMedia();
            GC.Collect();
        }
    }

    public class CalleeConductor : Conductor, ICalleeConductor
    {
        public CalleeConductor(ISignaller signaller)
            : base(signaller)
        {
            this.Signaller.ReceivedOffer += async (s, offer) =>
            {
                Logger.Log("Received offer...");

                this.PeerConnection = this.CreatePeerConnection();

                await this.MediaWrapper.LoadLocalMedia();
                this.MediaWrapper.AddLocalMediaToPeerConnection(this.PeerConnection);

                await this.PeerConnection.SetRemoteDescription(offer);
                var answer = await this.PeerConnection.CreateAnswer();
                await this.PeerConnection.SetLocalDescription(answer);

                await this.Signaller.SendAnswer(answer);
                await this.SubmitIceCandidatesAsync();

                Logger.Log("Sent answer...");
            };
        }
    
        IList<MediaWrapper.CaptureProfile> ICalleeConductor.GetCaptureProfiles()
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
        ISignaller Signaller { get; }
        Task Initialize(CoreDispatcher coreDispatcher);
        Task Shutdown();
    }

    public interface ICallerConductor : IBaseConductor
    {
        event EventHandler RemoteStreamAdded;
        Task StartCall();
    }

    public interface ICalleeConductor : IBaseConductor
    {
        IList<MediaWrapper.CaptureProfile> GetCaptureProfiles();
        void SetSelectedProfile(MediaWrapper.CaptureProfile captureProfile);
    }
}
