using System;
using System.Threading.Tasks;

namespace ApolloLensLibrary.Signalling
{
    /// <summary>
    /// Defines the most basic possible "signaller"
    /// interface. All that is required is the
    /// ability to send and recieve strings.
    /// </summary>
    public interface IBasicSignaller
    {
        event EventHandler<string> ReceivedMessage;
        Task SendMessage(string message);
    }
}
