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
using ApolloLensLibrary.Conducting;
using ApolloLensLibrary.Utilities;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ApolloLensClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public string ServerAddress { get; set; } = "10.0.0.192";
        public string ServerPort { get; set; } = "8888";
        public bool Logging { get; set; } = true;

        private Caller Caller { get; }

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            if (this.Logging)
            {
                Logger.WriteMessage += this.WriteLine;
            }

            this.Caller = new Caller(this.Dispatcher, this.RemoteVideo);
            this.Caller.RemoteStreamAdded += Caller_RemoteStreamAdded;
        }


        #region Utilities

        private async void WriteLine(string Message)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.OutputTextBox.Text += Message + Environment.NewLine;
            });
        }

        #endregion


        #region UI_Handlers        

        private void Caller_RemoteStreamAdded(object sender, EventArgs e)
        {
            this.OutputTextBox.Visibility = Visibility.Collapsed;
        }

        private void LoggerToggle_Click(object sender, RoutedEventArgs e)
        {
            if (this.Logging)
            {
                Logger.WriteMessage += this.WriteLine;
            }
            else
            {
                Logger.WriteMessage -= this.WriteLine;
            }
        }

        private async void ServerConnectButton_Click(object sender, RoutedEventArgs e)
        {
            this.ServerConnectButton.Visibility = Visibility.Collapsed;
            Logger.Log("Connecting to signalling server...");
            await this.Caller.ConnectToSignallingServer(this.ServerAddress, this.ServerPort);
        }

        private async void ClientConnectButton_Click(object sender, RoutedEventArgs e)
        {
            this.ClientConnectButton.Visibility = Visibility.Collapsed;
            Logger.Log("Initializing WebRtc...");
            await this.Caller.Initialize();
            Logger.Log("Connecting to WebRtc remote source...");
            await this.Caller.StartPeerConnection();
        }

        private async void PlainMessageButton_Click(object sender, RoutedEventArgs e)
        {
            await this.Caller.SayHi();
            Logger.Log("Said hello...");
        }

        #endregion
    }
}
