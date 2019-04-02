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

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var images = await DicomParser.GetStudyRaw();
            var server = new StreamSocketListener();
            server.ConnectionReceived += async (s, args) =>
            {
                try
                {
                    await DicomNetworking.SendStudyAsync(images, args.Socket);
                }
                catch
                {
                    return;
                }
            };

            await server.BindServiceNameAsync("8080");
            this.LoadingBlock.ToggleVisibility();
            this.ReadyBlock.ToggleVisibility();
        }
    }
}
