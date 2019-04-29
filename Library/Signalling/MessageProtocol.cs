using Newtonsoft.Json;
using System;
using Windows.Data.Json;

namespace ApolloLensLibrary.Signalling
{
    /// <summary>
    /// Wraps and unwrpas messages in JSON format, using an
    /// enumeration as a message type signifier.
    /// Allows messages to be distinguish by an enum instead
    /// of a raw string.
    /// </summary>
    /// <remarks>
    /// Requires project to be built in C# 7.3 to allow for
    /// where T : Enum
    /// </remarks>
    /// <typeparam name="T"></typeparam>
    public class MessageProtocol<T> where T : Enum
    {
        private string MessageTypeKey { get; } = "MessageType";
        private string MessageContentsKey { get; } = "MessageContents";

        /// <summary>
        /// Bundles the specified message and type together
        /// into a raw string, containing JSON.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="contents"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Parses a raw JSON string into a Message<T>, where
        /// the Message<t> contains the type (an Enum instance)
        /// and the message string itself.
        /// </summary>
        /// <param name="rawMessageJson"></param>
        /// <returns></returns>
        public Message<T> UnwrapMessage(string rawMessageJson)
        {
            if (!JsonObject.TryParse(rawMessageJson, out JsonObject messageJsonObject))
                throw new ArgumentException();

            if (!messageJsonObject.TryGetValue("MessageType", out IJsonValue messageTypeJsonValue))
                throw new ArgumentException();

            if (!messageJsonObject.TryGetValue("MessageContents", out IJsonValue messageContentsJsonValue))
                throw new ArgumentException();

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
