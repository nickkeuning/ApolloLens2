using Newtonsoft.Json;
using System;
using Windows.Data.Json;

namespace ApolloLensLibrary.Signalling
{
    public class MessageProtocol<T> where T : Enum
    {
        private string MessageTypeKey { get; } = "MessageType";
        private string MessageContentsKey { get; } = "MessageContents";

        public string WrapMessage(T type, string contents)
        {
            var messageTypeSerialized = JsonConvert.SerializeObject(type);
            JsonObject keyValuePairs = new JsonObject()
            {
                { this.MessageTypeKey, JsonValue.CreateStringValue(messageTypeSerialized) },
                { this.MessageContentsKey, JsonValue.CreateStringValue(contents ?? "") }
            };
            return keyValuePairs.Stringify();
        }

        public Message<T> UnwrapMessage(string rawMessageJson)
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

            var messageType = JsonConvert.DeserializeObject<T>(messageTypeJsonValue.GetString());
            var messageContents = messageContentsJsonValue.GetString();
            return new Message<T>(messageType, messageContents);
        }
    }

    public class Message<T> where T : Enum
    {
        public T Type { get; }
        public string Contents { get; }

        public Message(T type, string contents)
        {
            this.Type = type;
            this.Contents = contents;
        }
    }
}
