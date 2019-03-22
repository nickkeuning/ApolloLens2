using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using ApolloLensLibrary.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Networking.Sockets;
using System.Threading.Tasks;
using Windows.Storage.Streams;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ScanServer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private IDictionary<string, IEnumerable<WriteableBitmap>> Images;

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            this.Images = await DicomManager.GetStudy();

            var server = new StreamSocketListener();
            server.ConnectionReceived += this.Server_ConnectionReceived;
            await server.BindServiceNameAsync("8080");
            this.ReadyBlock.Visibility = Visibility.Visible;
        }

        private async void Server_ConnectionReceived(StreamSocketListener s, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            try
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    using (var writer = new DataWriter(args.Socket.OutputStream))
                    {
                        writer.WriteInt32(this.Images.Keys.Count);
                        await writer.StoreAsync();

                        foreach (var series in this.Images.Keys)
                        {
                            writer.WriteUInt32((uint)series.Length);
                            writer.WriteString(series);
                            writer.WriteInt32(this.Images[series].Count());
                            await writer.StoreAsync();

                            foreach (var image in this.Images[series])
                            {
                                writer.WriteInt32(image.PixelWidth);
                                writer.WriteInt32(image.PixelHeight);
                                writer.WriteUInt32(image.PixelBuffer.Length);
                                writer.WriteBuffer(image.PixelBuffer);
                                await writer.StoreAsync();
                            }
                        }
                    }
                });
            }
            catch
            {
                return;
            }
        }
    }
}
