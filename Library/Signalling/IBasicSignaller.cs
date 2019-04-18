using System;
using System.Threading.Tasks;

namespace ApolloLensLibrary.Signalling
{
    public interface IBasicSignaller
    {
        event EventHandler<string> ReceivedMessage;
        Task SendMessage(string message);
    }
}
