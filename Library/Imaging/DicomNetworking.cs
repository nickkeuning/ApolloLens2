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
    public class DicomNetworking
    {
        public event EventHandler<int> ReadyToLoad;
        public event EventHandler LoadedImage;
        public event EventHandler SentImage;

        private void OnReceivedNumImages(int numImages)
        {
            this.ReadyToLoad?.Invoke(this, numImages);
        }

        private void OnLoadedImage()
        {
            this.LoadedImage?.Invoke(this, EventArgs.Empty);
        }

        private void OnSentImage()
        {
            this.SentImage?.Invoke(this, EventArgs.Empty);
        }

        public async Task SendStudyAsync(IDictionary<string, IEnumerable<ImageTransferObject>> images, StreamSocket socket)
        {
            var numImages = images
                .Values
                .SelectMany(series => series)
                .Count();
                
            using (var writer = new DataWriter(socket.OutputStream))
            {
                writer.WriteInt32(numImages);
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
                        this.OnSentImage();
                    }
                }
            }
        }

        public async Task<IImageCollection> GetStudyAsync(string address)
        {
            var client = new StreamSocket();
            await client.ConnectAsync(new HostName(address), "8080");

            var imageCollection = ImageCollection.Create();

            using (var reader = new DataReader(client.InputStream))
            {
                await reader.LoadAsync(sizeof(int) * 2);

                var numImages = reader.ReadInt32();
                this.OnReceivedNumImages(numImages);

                var numSeries = reader.ReadInt32();
                foreach (var i in Util.Range(numSeries))
                {
                    await LoadSeriesAsync(reader, imageCollection);
                }
            }

            return imageCollection;
        }

        private async Task LoadSeriesAsync(DataReader reader, IImageCollection imageCollection)
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

        private async Task LoadImageAsync(DataReader reader, string seriesName, int position, IImageCollection imageCollection)
        {
            await reader.LoadAsync(sizeof(int) * 2 + sizeof(uint));
            var width = reader.ReadInt32();
            var height = reader.ReadInt32();
            var bufferLength = reader.ReadUInt32();

            await reader.LoadAsync(bufferLength);

            //var imageBytes = new byte[bufferLength];
            //reader.ReadBytes(imageBytes);

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
            this.OnLoadedImage();
        }
    }
}
