using System;

namespace ApolloLensLibrary.Utilities
{
    /// <summary>
    /// Allows injecting any method to log a message.
    /// Can log to user interface, a file, a stringbuilder,
    /// etc. Just bind a method / lamda to WriteMessage,
    /// then use Log or LogLine to fire that event, from
    /// anywhere.
    /// </summary>
    public class Logger
    {
        /// <summary>
        /// Action to take when Log or LogLine are
        /// called. Can be subscribed and unsubscibed
        /// at runtime. Allows for multiple logging
        /// methods to be turned on and off independently.
        /// </summary>
        public static event Action<string> WriteMessage;

        /// <summary>
        /// Log the message as is.
        /// </summary>
        /// <param name="message"></param>
        public static void Log(string message)
        {
            WriteMessage?.Invoke(message);
        }

        /// <summary>
        /// Log the message followed by a newline.
        /// </summary>
        /// <param name="message"></param>
        public static void LogLine(string message)
        {
            WriteMessage?.Invoke(message + Environment.NewLine);
        }
    }
}
