using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Newtonsoft.Json;
using Windows.Data.Json;
using System.Threading;

namespace ApolloLensLibrary.Signalling.Protocol
{
    public static class MessageProtocol
    {
        private static string MessageTypeKeyName { get; } = "MessageType";
        private static string MessageContentsKeyName { get; } = "MessageContents";

        public static async Task SendMessageToStreamAsync(IOutputStream outputStream, MessageType messageType, string messageContents)
        {
            var messageTypeSerialized = JsonConvert.SerializeObject(messageType);
            JsonObject keyValuePairs = new JsonObject()
            {
                { MessageProtocol.MessageTypeKeyName, JsonValue.CreateStringValue(messageTypeSerialized) },
                { MessageProtocol.MessageContentsKeyName, JsonValue.CreateStringValue(messageContents) }
            };
            var message = keyValuePairs.Stringify();

            await TcpProtocol.SendStringToStreamAsync(outputStream, message);
        }


        public static async Task<RtcSignallingMessage> ReadMessageFromStreamAsync(IInputStream inputStream, CancellationToken cancellationToken)
        {

            var rawMessageJson = await TcpProtocol.ReadStringFromStreamAsync(inputStream, cancellationToken);

            if (!JsonObject.TryParse(rawMessageJson, out JsonObject messageJsonObject))
            {
                throw new Exception("Unable to parse answer from server into valid json.");
            }

            if (!messageJsonObject.TryGetValue("MessageType", out IJsonValue messageTypeJsonValue))
            {
                throw new Exception("Unable to find message type in server message.");
            }

            if (!messageJsonObject.TryGetValue("MessageContents", out IJsonValue messageContentsJsonValue))
            {
                throw new Exception("Unable to find message contents in server message.");
            }

            var messageType = JsonConvert.DeserializeObject<MessageType>(messageTypeJsonValue.GetString());
            var messageContents = messageContentsJsonValue.GetString();
            return new RtcSignallingMessage(messageType, messageContents);
        }

        public static bool IsPingAnswer(string rawJson)
        {
            if (!JsonObject.TryParse(rawJson, out JsonObject jsonObj))
            {
                return false;
            }

            if (!jsonObj.TryGetValue("MessageType", out IJsonValue jsonValue))
            {
                return false;
            }

            var messageType = JsonConvert.DeserializeObject<MessageType>(jsonValue.GetString());
            return messageType == MessageType.PingAnswer;
        }

        public enum MessageType
        {
            Offer,
            Answer,
            IceCandidate,
            IceAnswer,
            Plain,
            Ping,
            PingAnswer
        }

        public class RtcSignallingMessage
        {
            public MessageType Type { get; }
            public string Contents { get; }

            public RtcSignallingMessage(MessageType type, string contents)
            {
                this.Type = type;
                this.Contents = contents;
            }
        }
    }
}
