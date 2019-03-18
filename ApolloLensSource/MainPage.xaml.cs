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
        private Callee Callee { get; set; }

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            if (this.Logging)
            {
                Logger.WriteMessage += this.WriteLine;
            }
            this.Callee = new Callee();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            await this.Callee.Initialize(this.Dispatcher);
            var res = this.Callee.CaptureProfiles;
            this.CaptureFormatComboBox.ItemsSource = res;
        }

        #region Utilities

        private async void WriteLine(string Message)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.OutputTextBox.Text += Message + Environment.NewLine;
            });
        }

        private void ToggleValidWhenDisconnectedControls()
        {
            this.ConnectToServerButton.ToggleVisibility();
            this.RemoteServerToggle.ToggleVisibility();
            this.CaptureFormatComboBox.ToggleVisibility();
        }

        private void ToggleValidWhenConnectedControls()
        {
            this.DisconnectFromServerButton.ToggleVisibility();
            this.SayHiButton.ToggleVisibility();
        }

        #endregion

        #region UI_Handlers

        private async void ConnectToServer_Click(object sender, RoutedEventArgs e)
        {
            this.ToggleValidWhenDisconnectedControls();

            await this.Callee.ConnectToSignallingServer(this.ServerAddress);

            this.ToggleValidWhenConnectedControls();
        }

        private async void SayHiButton_Click(object sender, RoutedEventArgs e)
        {
            await this.Callee.SayHi();
        }

        private void DisconnectFromServerButton_Click(object sender, RoutedEventArgs e)
        {
            this.ToggleValidWhenConnectedControls();

            this.Callee.DisconnectFromSignallingServer();

            this.ToggleValidWhenDisconnectedControls();
        }

        private void CaptureFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProfile = (this.CaptureFormatComboBox.SelectedItem as Wrapper.CaptureProfile);
            this.Callee.SetSelectedProfile(selectedProfile);
        }

        #endregion
    }
}
