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

        private string ServerAddress => this.ConnectAzure ? ServerConfig.AwsAddress : $"ws://{this.CustomAddress}:8080/";

        private ICallerConductor Conductor { get; }

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();

            Logger.WriteMessage += async (message) =>
            {
                await this.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    this.OutputTextBox.Text += message + Environment.NewLine;
                });
            };

            Application.Current.Suspending += async (s, e) =>
            {
                await this.Conductor.Signaller.SendShutdown();
                await this.Conductor.Shutdown();
            };            

            this.Conductor = this.BuildConductor();

            if (!ApplicationData.Current.LocalSettings.Values.TryGetValue(this.CustomServerSettingsKey, out object value))
            {
                ApplicationData.Current.LocalSettings.Values["CustomServerAddress"] = null;
            }
            this.CustomAddress = (string)value;
        }

        private ISignaller BuildSignaller()
        {
            var signaller = WebSocketSignaller.CreateSignaller();

            signaller.ConnectionFailed += (s, a) =>
            {
                Logger.Log("Connecting to signalling server failed. Try again.");
            };

            signaller.ReceivedPlainMessage += (s, a) =>
            {
                Logger.Log($"Received plain message: {a}");
            };

            signaller.ReceivedShutdown += async (s, a) =>
            {
                Logger.Log("Received shutdown message from peer.");
                await this.Conductor.Shutdown();
            };

            return signaller;
        }

        private ICallerConductor BuildConductor()
        {
            var signaller = this.BuildSignaller();
            var conductor = new CallerConductor(signaller, this.RemoteVideo);

            conductor.RemoteStreamAdded += (s, a) =>
            {
                this.NotInCall.ToggleVisibility();
            };

            return conductor;
        }


        #region UI_Handlers        

        private async void ServerConnectButton_Click(object sender, RoutedEventArgs e)
        {
            this.StartupSettings.ToggleVisibility();
            await this.Conductor.Signaller.ConnectToServer(this.ServerAddress);
            this.ConnectedOptions.ToggleVisibility();
        }

        private async void SayHiButton_Click(object sender, RoutedEventArgs e)
        {
            var message = "Hello, World!";
            await this.Conductor.Signaller.SendPlainMessage(message);
            Logger.Log($"Sent {message} to any connected peers");
        }

        private async void SourceConnectButton_Click(object sender, RoutedEventArgs e)
        {
            Logger.Log("Initializing WebRTC...");
            await this.Conductor.Initialize(this.Dispatcher);
            Logger.Log("Done.");

            Logger.Log("Starting connection to source...");
            await this.Conductor.StartCall();
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
