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
        private ISourceConductor SourceConductor { get; set; }
        private IUISignaller Signaller { get; set; }

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
                await this.Signaller.SendShutdown();
                await this.SourceConductor.Shutdown();
            };

            this.Signaller = this.BuildSignaller();
            this.SourceConductor = this.BuildConductor(this.Signaller);
        }

        private IUISignaller BuildSignaller()
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
                await this.SourceConductor.Shutdown();
            };

            return signaller;
        }

        private ISourceConductor BuildConductor(IUISignaller signaller)
        {
            return new SourceConductor(signaller as ISourceSignaller);
        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            Logger.Log("Initializing WebRTC...");
            await this.SourceConductor.Initialize(this.Dispatcher);
            Logger.Log("Done.");

            var profiles = this.SourceConductor.GetCaptureProfiles();
            this.CaptureFormatComboBox.ItemsSource = profiles;
            this.CaptureFormatComboBox.SelectedIndex = 0;
        }

        #region UI_Handlers

        private async void ConnectToServer_Click(object sender, RoutedEventArgs e)
        {
            this.NotConnected.ToggleVisibility();
            await this.SourceConductor.Signaller.ConnectToServer(this.ServerAddress);
            this.Connected.ToggleVisibility();
        }

        private async void SayHiButton_Click(object sender, RoutedEventArgs e)
        {
            var message = "Hello, World!";
            await this.Signaller.SendPlainMessage(message);
            Logger.Log($"Send message: {message} to connected peers");
        }

        private void DisconnectFromServerButton_Click(object sender, RoutedEventArgs e)
        {
            this.Connected.ToggleVisibility();
            this.SourceConductor.Signaller.DisconnectFromServer();
            this.NotConnected.ToggleVisibility();
        }

        private void CaptureFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProfile = (this.CaptureFormatComboBox.SelectedItem as MediaWrapper.CaptureProfile);
            this.SourceConductor.SetSelectedProfile(selectedProfile);
        }

        #endregion
    }
}
