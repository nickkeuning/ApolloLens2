using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Networking;
using Windows.Storage.Streams;
using ApolloLensLibrary.Utilities;
using System.Runtime.InteropServices.WindowsRuntime;


namespace ApolloLensLibrary.Imaging
{
    public static class DicomNetworking
    {
        public static async Task SendStudyAsync(IDictionary<string, IEnumerable<ImageTransferObject>> images, StreamSocket socket)
        {
            using (var writer = new DataWriter(socket.OutputStream))
            {
                writer.WriteInt32(images.Keys.Count);
                await writer.StoreAsync();

                foreach (var series in images.Keys)
                {
                    writer.WriteUInt32((uint)series.Length);
                    writer.WriteString(series);
                    writer.WriteInt32(images[series].Count());
                    await writer.StoreAsync();

                    foreach (var image in images[series])
                    {
                        writer.WriteInt32(image.Width);
                        writer.WriteInt32(image.Height);
                        writer.WriteUInt32(image.Image.AsBuffer().Length);
                        writer.WriteBuffer(image.Image.AsBuffer());
                        await writer.StoreAsync();
                    }
                }
            }
        }

        public static async Task<IDicomStudy> GetStudyAsync(string address)
        {
            var client = new StreamSocket();
            await client.ConnectAsync(new HostName(address), "8080");

            var imageCollection = new ImageCollection();

            using (var reader = new DataReader(client.InputStream))
            {
                await reader.LoadAsync(sizeof(int));
                var numSeries = reader.ReadInt32();
                foreach (var i in Util.Range(numSeries))
                {
                    await LoadSeriesAsync(reader, imageCollection);
                }
            }

            return imageCollection;
        }

        private static async Task LoadSeriesAsync(DataReader reader, IDicomStudy imageCollection)
        {
            await reader.LoadAsync(sizeof(int));
            var nameLength = reader.ReadUInt32();

            await reader.LoadAsync(nameLength);
            var seriesName = reader.ReadString(nameLength);

            await reader.LoadAsync(sizeof(int));
            var numImages = reader.ReadInt32();

            imageCollection.CreateSeries(seriesName, numImages);
            foreach (var position in Util.Range(numImages))
            {
                await LoadImageAsync(reader, seriesName, position, imageCollection);
            }
        }

        private static async Task LoadImageAsync(DataReader reader, string seriesName, int position, IDicomStudy imageCollection)
        {
            await reader.LoadAsync(sizeof(int) * 2 + sizeof(uint));
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var bufferLength = reader.ReadUInt32();

            await reader.LoadAsync(bufferLength);
            var buffer = reader.ReadBuffer(bufferLength);

            var imageBytes = new byte[bufferLength];
            using (var source = buffer.AsStream())
            {
                using (var destination = imageBytes.AsBuffer().AsStream())
                {
                    await source.CopyToAsync(destination);
                }
            }
            imageCollection.AddImageToSeries(
                imageBytes, seriesName, position, width, height);
        }


    }
}
