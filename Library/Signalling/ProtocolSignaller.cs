using System;
using System.Threading.Tasks;

namespace ApolloLensLibrary.Signalling
{
    public class ProtocolSignaller<T> where T : Enum
    {
        private IBasicSignaller Signaller { get; }
        private MessageProtocol<T> Protocol { get; }

        public ProtocolSignaller(IBasicSignaller signaller, MessageProtocol<T> protocol)
        {
            this.Signaller = signaller;
            this.Protocol = protocol;

            this.Signaller.ReceivedMessage += (sender, message) =>
            {
                var result = this.Protocol.UnwrapMessage(message);
                this.ReceivedMessage?.Invoke(this, result);
            };
        }

        public event EventHandler<Message<T>> ReceivedMessage;

        public async Task SendMessage(T type, string contents)
        {
            var wrappedMessage = this.Protocol.WrapMessage(type, contents);
            await this.Signaller.SendMessage(wrappedMessage);
        }
    }
}
