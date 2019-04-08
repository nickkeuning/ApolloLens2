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
using ApolloLensLibrary.Signalling;
using Windows.UI.Core;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ApolloLensSource
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public bool Logging { get; set; } = true;
        public bool ConnectAzure { get; set; } = true;

        private string ServerAddress => this.ConnectAzure ? ServerConfig.AwsAddress : ServerConfig.LocalAddress;

        private ICalleeConductor Conductor { get; }

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

        private ICalleeConductor BuildConductor()
        {
            var signaller = this.BuildSignaller();
            return new CalleeConductor(signaller);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            Logger.Log("Initializing WebRTC...");
            await this.Conductor.Initialize(this.Dispatcher);
            Logger.Log("Done.");

            var profiles = this.Conductor.GetCaptureProfiles();
            this.CaptureFormatComboBox.ItemsSource = profiles;
            this.CaptureFormatComboBox.SelectedIndex = 0;
        }

        #region UI_Handlers

        private async void ConnectToServer_Click(object sender, RoutedEventArgs e)
        {
            this.NotConnected.ToggleVisibility();
            await this.Conductor.Signaller.ConnectToServer(this.ServerAddress);
            this.Connected.ToggleVisibility();
        }

        private async void SayHiButton_Click(object sender, RoutedEventArgs e)
        {
            var message = "Hello, World!";
            await this.Conductor.Signaller.SendPlainMessage(message);
            Logger.Log($"Send message: {message} to connected peers");
        }

        private void DisconnectFromServerButton_Click(object sender, RoutedEventArgs e)
        {
            this.Connected.ToggleVisibility();
            this.Conductor.Signaller.DisconnectFromServer();
            this.NotConnected.ToggleVisibility();
        }

        private void CaptureFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProfile = (this.CaptureFormatComboBox.SelectedItem as MediaWrapper.CaptureProfile);
            this.Conductor.SetSelectedProfile(selectedProfile);
        }

        #endregion
    }
}
