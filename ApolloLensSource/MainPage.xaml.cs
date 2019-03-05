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


        private string ServerAddress => this.ConnectAzure ? "wss://apollosignalling.azurewebsites.net/" : "ws://localhost/";
        private Callee Callee { get; set; }

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            if (this.Logging)
            {
                Logger.WriteMessage += this.WriteLine;
            }
            this.Callee = new Callee(this.Dispatcher);
        }

        #region Utilities

        private async void WriteLine(string Message)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.OutputTextBox.Text += Message + Environment.NewLine;
            });
        }

        private void ToggleDisconnectedStateButtons()
        {
            this.ConnectToServerButton.ToggleVisibility();
            this.RemoteServerToggle.ToggleVisibility();
        }

        private void ToggleConnectedStateButtons()
        {
            this.DisconnectFromServerButton.ToggleVisibility();
            this.SayHiButton.ToggleVisibility();
            this.SetToHighestButton.ToggleVisibility();
            this.SetToLowestButton.ToggleVisibility();
        }

        #endregion

        #region UI_Handlers

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

        private async void ConnectToServer_Click(object sender, RoutedEventArgs e)
        {
            this.ToggleDisconnectedStateButtons();

            await this.Callee.ConnectToSignallingServer(this.ServerAddress);
            await this.Callee.Initialize();

            this.ToggleConnectedStateButtons();
        }

        private async void SayHiButton_Click(object sender, RoutedEventArgs e)
        {
            await this.Callee.SayHi();
        }

        private void DisconnectFromServerButton_Click(object sender, RoutedEventArgs e)
        {
            this.ToggleConnectedStateButtons();

            this.Callee.DisconnectFromSignallingServer();

            this.ToggleDisconnectedStateButtons();
        }        

        private async void SetToHighestButton_Click(object sender, RoutedEventArgs e)
        {
            await this.Callee.SetToHighest();
            Logger.Log("Set bitrate to highest available...");
        }        

        private async void SetToLowestButton_Click(object sender, RoutedEventArgs e)
        {
            await this.Callee.SetToLowest();
            Logger.Log("Set bitrate to lowest available...");
        }

        #endregion
    }
}
