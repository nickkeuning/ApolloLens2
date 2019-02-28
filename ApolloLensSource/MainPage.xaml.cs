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
using ApolloLensLibrary.SignallingServer;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ApolloLensSource
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public bool Logging { get; set; } = true;
        public string Port { get; set; } = "8888";
        private Server Server { get; set; }
        private Callee Callee { get; set; }

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            if (this.Logging)
            {
                Logger.WriteMessage += this.WriteLine;
            }

            this.Server = new Server(this.Port);
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

        private async void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            this.StartServerButton.Visibility = Visibility.Collapsed;
            await this.Server.Start();
            await this.Callee.ConnectToSignallingServer("localhost", this.Port);
            await this.Callee.Initialize();
        }

        private async void PlainMessageButton_Click(object sender, RoutedEventArgs e)
        {
            await this.Callee.SayHi();
        }

        #endregion
    }
}
