using System;
using System.Threading.Tasks;

namespace ApolloLensLibrary.Signalling
{

    /// <summary>
    /// Combines the concepts of a message protocol and a
    /// basic signaller. Wraps and unwraps messages using
    /// the protocol, and sends and receives them over the
    /// basic signaller.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ProtocolSignaller<T> where T : Enum
    {
        private IBasicSignaller Signaller { get; }
        private MessageProtocol<T> Protocol { get; }

        /// <summary>
        /// Constructor injects the needed signaller
        /// and protocol.
        /// </summary>
        /// <param name="signaller"></param>
        /// <param name="protocol"></param>
        public ProtocolSignaller(IBasicSignaller signaller, MessageProtocol<T> protocol)
        {
            this.Signaller = signaller;
            this.Protocol = protocol;

            // Listen for when the basic signaller receives a
            // message.
            this.Signaller.ReceivedMessage += (sender, message) =>
            {
                // Use the protocol to unwrap the raw string
                // into a Message<T>
                var result = this.Protocol.UnwrapMessage(message);
                this.ReceivedMessage?.Invoke(this, result);
            };
        }

        public event EventHandler<Message<T>> ReceivedMessage;

        public async Task SendMessage(T type, string contents)
        {
            // Use the protocol to wrap the specified message
            // and message type into a Message<T>
            var wrappedMessage = this.Protocol.WrapMessage(type, contents);
            await this.Signaller.SendMessage(wrappedMessage);
        }
    }
}
