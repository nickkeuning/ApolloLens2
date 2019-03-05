using Newtonsoft.Json;
using System;
using Windows.Data.Json;

namespace ApolloLensLibrary.Signalling
{
    public static class MessageProtocol
    {
        private static string MessageTypeKeyName { get; } = "MessageType";
        private static string MessageContentsKeyName { get; } = "MessageContents";

        public static string WrapMessage(string messageContents, MessageType messageType)
        {
            var messageTypeSerialized = JsonConvert.SerializeObject(messageType);
            JsonObject keyValuePairs = new JsonObject()
            {
                { MessageProtocol.MessageTypeKeyName, JsonValue.CreateStringValue(messageTypeSerialized) },
                { MessageProtocol.MessageContentsKeyName, JsonValue.CreateStringValue(messageContents ?? "") }
            };
            return keyValuePairs.Stringify();
        }

        public static RtcSignallingMessage UnwrapMessage(string rawMessageJson)
        {
            if (!JsonObject.TryParse(rawMessageJson, out JsonObject messageJsonObject))
            {
                throw new ArgumentException();
            }

            if (!messageJsonObject.TryGetValue("MessageType", out IJsonValue messageTypeJsonValue))
            {
                throw new ArgumentException();
            }

            if (!messageJsonObject.TryGetValue("MessageContents", out IJsonValue messageContentsJsonValue))
            {
                throw new ArgumentException();
            }

            var messageType = JsonConvert.DeserializeObject<MessageType>(messageTypeJsonValue.GetString());
            var messageContents = messageContentsJsonValue.GetString();
            return new RtcSignallingMessage(messageType, messageContents);
        }

        public enum MessageType
        {
            Offer,
            Answer,
            IceCandidate,
            Plain,
            Shutdown
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
