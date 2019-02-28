using System;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using System.Threading;

namespace ApolloLensLibrary.Signalling.Protocol
{
    static class TcpProtocol
    {
        /// <summary>
        /// Send string to the stream asynch.
        /// </summary>
        /// <param name="outputStream"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static async Task SendStringToStreamAsync(IOutputStream outputStream, string message)
        {
            using (var writer = new DataWriter(outputStream))
            {
                writer.WriteUInt32(writer.MeasureString(message));

                writer.WriteString(message);
                await writer.StoreAsync();

                writer.DetachStream();
            }
        }


        /// <summary>
        /// Read string from the stream async
        /// </summary>
        /// <param name="inputStream"></param>
        /// <returns></returns>
        public static async Task<string> ReadStringFromStreamAsync(IInputStream inputStream, CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                ct.ThrowIfCancellationRequested();
            }

            using (var reader = new DataReader(inputStream))
            {
                try
                {
                    await reader.LoadAsync(sizeof(uint)).AsTask(ct);
                    var numBytes = reader.ReadUInt32();

                    await reader.LoadAsync(numBytes).AsTask(ct);
                    return reader.ReadString(numBytes);
                }
                catch (TaskCanceledException)
                {
                    ct.ThrowIfCancellationRequested();
                    return null;
                }
            }
        }
    }
}
