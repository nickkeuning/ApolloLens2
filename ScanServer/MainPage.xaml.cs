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
using ApolloLensLibrary.Utilities;
using Windows.UI.Core;


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

        IDictionary<string, IEnumerable<ImageTransferObject>> images;
        DicomNetworking dicomSender;
        StreamSocketListener server;

        private async void Load_Click(object sender, RoutedEventArgs e)
        {
            await this.LoadImages();
            await this.InitializeServer();

            this.WaitingBlock.Show();           
        }

        private async Task InitializeServer()
        {
            this.server = new StreamSocketListener();
            this.server.ConnectionReceived += this.Server_ConnectionReceived;

            this.dicomSender = new DicomNetworking();
            this.dicomSender.SentImage += this.Sender_SentImage;

            await this.server.BindServiceNameAsync("8080");
        }

        private async void Sender_SentImage(object sender, EventArgs e)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.LoadingBar.Value++;
            });
        }

        private async void Server_ConnectionReceived(
            StreamSocketListener sender,
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                this.LoadingBar.Value = 0;
                this.WaitingBlock.Hide();
                this.LoadingBar.Show();
                this.SendingBlock.Show();

                try
                {
                    await this.dicomSender.SendStudyAsync(this.images, args.Socket);
                }
                finally
                {
                    this.LoadingBar.Hide();
                    this.SendingBlock.Hide();
                    this.WaitingBlock.Show();
                }
            });

        }

        private async Task LoadImages()
        {
            this.Load.Hide();
            this.LoadingBlock.Show();

            var parser = new DicomParser();
            parser.ReadyToLoad += async (s, num) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.LoadingBar.Show();

                    this.LoadingBar.Maximum = num;
                });
            };

            parser.LoadedImage += async (s, a) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.LoadingBar.Value++;
                });
            };

            this.images = await parser.GetStudyRaw();

            this.LoadingBar.Hide();
            this.LoadingBlock.Hide();
        }
    }
}
