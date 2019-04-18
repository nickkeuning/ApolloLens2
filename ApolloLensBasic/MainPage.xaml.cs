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
using ApolloLensLibrary.Utilities;
using WebRtcImplOld;
using ApolloLensLibrary.WebRtc;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ApolloLensBasic
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        //private MediaWrapper Wrapper { get; }
        private IConductor conductor { get; } = Conductor.Instance;

        public MainPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            var config = new ConductorConfig()
            {
                CoreDispatcher = this.Dispatcher,
                LocalVideo = this.LocalVideo
            };

            await this.conductor.Initialize(config);

            var opts = new MediaOptions(
                new MediaOptions.Init()
                {
                    LocalLoopback = true
                });
            this.conductor.SetMediaOptions(opts);

            var res = this.conductor.CaptureProfiles;
            this.CaptureFormatComboBox.ItemsSource = res;
            this.CaptureFormatComboBox.SelectedIndex = 0;
        }

        private void ToggleVisibilities()
        {
            this.LocalVideo.ToggleVisibility();
            this.HideVideo.ToggleVisibility();
            this.ShowVideo.ToggleVisibility();
            this.CaptureFormatComboBox.ToggleVisibility();
        }

        private async void ShowVideo_Click(object sender, RoutedEventArgs e)
        {
            await this.conductor.StartCall();
            this.ToggleVisibilities();
        }

        private async void HideVideo_Click(object sender, RoutedEventArgs e)
        {
            await this.conductor.Shutdown();
            this.ToggleVisibilities();
        }

        private void CaptureFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProfile = (this.CaptureFormatComboBox.SelectedItem as CaptureProfile);
            this.conductor.SetSelectedProfile(selectedProfile);
        }
    }
}
