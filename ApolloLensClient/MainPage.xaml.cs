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


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ApolloLensClient
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public bool Logging { get; set; } = true;
        public bool ConnectAzure { get; set; } = true;
        public string CustomAddress { get; set; }

        private string CustomServerSettingsKey { get; } = "CustomServerAddress";
        private string ServerAddress => this.ConnectAzure ? "wss://apollosignalling.azurewebsites.net/" : $"ws://{this.CustomAddress}/";


        private Caller Caller { get; }

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            Application.Current.Suspending += this.Current_Suspending;

            if (this.Logging)
            {
                Logger.WriteMessage += this.WriteLine;
            }

            this.Caller = new Caller(this.Dispatcher, this.RemoteVideo);
            this.Caller.RemoteStreamAdded += this.Caller_RemoteStreamAdded;

            if (!ApplicationData.Current.LocalSettings.Values.TryGetValue(this.CustomServerSettingsKey, out object value))
            {
                ApplicationData.Current.LocalSettings.Values["CustomServerAddress"] = null;
            }
            this.CustomAddress = (string)value;
        }

        private async void Current_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            await this.Caller.ShutDown();
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
            this.ServerConnectButton.ToggleVisibility();
            this.RemoteServerToggle.ToggleVisibility();
            this.CustomServerAddressBox.Hide();

            Logger.Log("Connecting to signalling server...");
            await this.Caller.ConnectToSignallingServer(this.ServerAddress);

            this.SourceConnectButton.ToggleVisibility();
            this.SayHiButton.ToggleVisibility();
        }

        private async void SayHiButton_Click(object sender, RoutedEventArgs e)
        {
            await this.Caller.SayHi();
            Logger.Log("Said hello...");
        }

        private async void SourceConnectButton_Click(object sender, RoutedEventArgs e)
        {
            this.SourceConnectButton.Visibility = Visibility.Collapsed;
            Logger.Log("Initializing WebRtc...");
            await this.Caller.Initialize();
            Logger.Log("Connecting to WebRtc remote source...");
            await this.Caller.StartPeerConnection();

            this.SettingsPanel.ToggleVisibility();
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
