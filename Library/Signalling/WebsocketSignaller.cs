using System;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace ApolloLensLibrary.Signalling
{
    /// <summary>
    /// An implementation of the IBasicSignaller interface
    /// using websockets. Very simple code. Very simple
    /// corresponding server is also possible.
    /// </summary>
    public class WebsocketSignaller : IBasicSignaller
    {
        private MessageWebSocket WebSocket { get; set; }

        public event EventHandler ConnectionFailed;
        public event EventHandler<string> ReceivedMessage;

        /// <summary>
        /// Connect to the server at the specified address.
        /// </summary>
        /// <param name="address">
        /// Needs to be in the form "ws://..." or "wss://..."
        /// </param>
        /// <returns></returns>
        public async Task ConnectToServer(string address)
        {
            try
            {
                this.WebSocket = new MessageWebSocket();
                this.WebSocket.Control.MessageType = SocketMessageType.Utf8;
                this.WebSocket.MessageReceived += this.WebSocket_MessageReceived;
                this.WebSocket.Closed += this.WebSocket_Closed;
                await this.WebSocket.ConnectAsync(new Uri(address));
            }
            catch
            {
                this.ConnectionFailed?.Invoke(this, EventArgs.Empty);
            }
        }

        public void DisconnectFromServer()
        {
            this.WebSocket.Close(1000, "");
        }

        public async Task SendMessage(string message)
        {
            // This should probably throw an exception
            // instead of quietly returning.
            if (this.WebSocket == null)
                return;

            // Use a datawriter to write the specified 
            // message to the websocket.
            using (var dataWriter = new DataWriter(this.WebSocket.OutputStream))
            {
                dataWriter.WriteString(message);
                await dataWriter.StoreAsync();
                dataWriter.DetachStream();
            }
        }



        private void WebSocket_MessageReceived(MessageWebSocket sender, MessageWebSocketMessageReceivedEventArgs args)
        {
            try
            {
                // Use a datareader to read the message 
                // out of the websocket args.
                using (DataReader dataReader = args.GetDataReader())
                {
                    dataReader.UnicodeEncoding = UnicodeEncoding.Utf8;
                    var rawMessage = dataReader.ReadString(dataReader.UnconsumedBufferLength);
                    this.ReceivedMessage?.Invoke(this, rawMessage);
                }
            }
            catch (Exception ex)
            {
                // This should probably rethrow since exceptions
                // are currently silenced.
                Windows.Web.WebErrorStatus webErrorStatus = 
                    WebSocketError.GetStatus(ex.GetBaseException().HResult);
            }
        }

        private void WebSocket_Closed(IWebSocket sender, WebSocketClosedEventArgs args)
        {
            this.WebSocket.Dispose();
            this.WebSocket = null;
        }
    }
}
