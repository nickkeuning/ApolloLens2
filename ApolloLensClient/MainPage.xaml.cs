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
using Windows.Storage;
using Windows.UI.Core;
using ApolloLensLibrary.Signalling;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ApolloLensClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public bool ConnectAzure { get; set; } = true;

        private string CustomServerSettingsKey { get; } = "CustomServerAddress";
        public string CustomAddress { get; set; }

        private string ServerAddress => this.ConnectAzure ? ServerConfig.AwsAddress : $"ws://{this.CustomAddress}/";

        private IClientConductor ClientConductor { get; }
        private IUISignaller Signaller { get; }

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            Application.Current.Suspending += async (s, e) =>
            {
                await this.ClientConductor.Shutdown();
            };

            Logger.WriteMessage += async (message) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.OutputTextBox.Text += message + Environment.NewLine;
                });
            };

            this.ClientConductor = Conductor.CreateClient(this.RemoteVideo);
            this.InitializeConductor();

            this.Signaller = WebSocketSignaller.CreateSignaller();
            this.InitializeSignaller();

            if (!ApplicationData.Current.LocalSettings.Values.TryGetValue(this.CustomServerSettingsKey, out object value))
            {
                ApplicationData.Current.LocalSettings.Values["CustomServerAddress"] = null;
            }
            this.CustomAddress = (string)value;
        }

        private void InitializeSignaller()
        {
            this.Signaller.ConnectionFailed += (s, a) =>
            {
                Logger.Log("Connecting to signalling server failed. Try again.");
            };

            this.Signaller.ReceivedPlainMessage += (s, a) =>
            {
                Logger.Log($"Received plain message: {a}");
            };

            this.Signaller.ReceivedShutdown += async (s, a) =>
            {
                Logger.Log("Received shutdown message from peer.");
                await this.ClientConductor.Shutdown();
            };
        }

        private void InitializeConductor()
        {
            this.ClientConductor.RemoteStreamAdded += (s, a) =>
            {
                this.NotInCall.ToggleVisibility();
            };
        }


        #region UI_Handlers        

        private async void ServerConnectButton_Click(object sender, RoutedEventArgs e)
        {
            this.StartupSettings.ToggleVisibility();

            Logger.Log("Connecting to signalling server...");

            await this.Signaller.ConnectToServer(this.ServerAddress);

            Logger.Log("Connected to signalling server.");

            this.ConnectedOptions.ToggleVisibility();
        }

        private async void SayHiButton_Click(object sender, RoutedEventArgs e)
        {
            var message = "Hello, World!";
            await this.Signaller.SendPlainMessage(message);
            Logger.Log($"Sent {message} to any connected peers");
        }

        private async void SourceConnectButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("Initializing WebRTC...");
            this.ClientConductor.SetSignaller(this.Signaller as IClientSignaller);
            await this.ClientConductor.Initialize(this.Dispatcher);
            Logger.Log("Done.");

            Logger.Log("Starting connection to source...");
            await this.ClientConductor.ConnectToSource();
        }

        private void CustomServerAddressBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            ApplicationData.Current.LocalSettings.Values[this.CustomServerSettingsKey] = textBox.Text;
        }


        private void RemoteServerToggle_Toggled(object sender, RoutedEventArgs e)
        {
            this.CustomServerAddressBox.ToggleVisibility();
        }

        #endregion
    }
}
