using System;

namespace ApolloLensLibrary.Utilities
{
    public class Logger
    {
        public static event Action<string> WriteMessage;

        public static void Log(string message)
        {
            WriteMessage?.Invoke(message);
        }

        public static void LogLine(string message)
        {
            WriteMessage?.Invoke(message + Environment.NewLine);
        }
    }
}
