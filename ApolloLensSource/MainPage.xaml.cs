using ApolloLensLibrary.Signalling;
using ApolloLensLibrary.Utilities;
using ApolloLensLibrary.WebRtc;
using System;
using WebRtcImplOld;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ApolloLensSource
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IConductor conductor { get; } = Conductor.Instance;

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
                await this.conductor.UISignaller.SendShutdown();
                await this.conductor.Shutdown();
            };

        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            var signaller = new WebsocketSignaller();

            this.ConnectToServerButton.Click += async (s, a) =>
            {
                this.NotConnected.Hide();
                await signaller.ConnectToServer(ServerConfig.AwsAddress);
                this.Connected.Show();
            };

            this.DisconnectFromServerButton.Click += (s, a) =>
            {
                this.Connected.Hide();
                signaller.DisconnectFromServer();
                this.NotConnected.Show();
            };

            var config = new ConductorConfig()
            {
                CoreDispatcher = this.Dispatcher,
                Signaller = signaller
            };

            Logger.Log("Initializing WebRTC...");
            await this.conductor.Initialize(config);
            Logger.Log("Done.");

            var opts = new MediaOptions(
                new MediaOptions.Init()
                {
                    SendVideo = true,
                });
            this.conductor.SetMediaOptions(opts);

            this.conductor.UISignaller.ReceivedShutdown += async (s, a) =>
            {
                await this.conductor.Shutdown();
            };

            this.conductor.UISignaller.ReceivedPlain += (s, message) =>
            {
                Logger.Log(message);
            };

            this.MediaDeviceComboBox.ItemsSource = this.conductor.MediaDevices;
            this.MediaDeviceComboBox.SelectedIndex = 0;

            this.CaptureFormatComboBox.ItemsSource = this.conductor.CaptureProfiles;
            this.CaptureFormatComboBox.SelectedIndex = 0;
        }

        #region UI_Handlers

        private async void SayHiButton_Click(object sender, RoutedEventArgs e)
        {
            var message = "Hello, World!";
            await this.conductor.UISignaller.SendPlain(message);
            Logger.Log($"Send message: {message} to connected peers");
        }

        private void CaptureFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProfile = (this.CaptureFormatComboBox.SelectedItem as CaptureProfile);
            this.conductor.SetSelectedProfile(selectedProfile);
        }

        #endregion

        private void MediaDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mediaDevice = (this.MediaDeviceComboBox.SelectedItem as MediaDevice);
            this.conductor.SetSelectedVideoDevice(mediaDevice);

            this.CaptureFormatComboBox.ItemsSource = this.conductor.CaptureProfiles;
            this.CaptureFormatComboBox.SelectedIndex = 0;
        }
    }
}
