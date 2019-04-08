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

namespace ApolloLensBasic
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaWrapper Wrapper { get; }

        public MainPage()
        {
            this.InitializeComponent();
            this.Wrapper = MediaWrapper.Instance;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs args)
        {
            await this.Wrapper.Initialize(this.Dispatcher);
            var res = this.Wrapper.CaptureProfiles;
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
            await this.Wrapper.LoadLocalMedia();
            await this.Wrapper.BindLocalVideo(this.LocalVideo);
            this.ToggleVisibilities();
        }

        private async void HideVideo_Click(object sender, RoutedEventArgs e)
        {
            this.ToggleVisibilities();
            await this.Wrapper.DestroyLocalMedia();
        }

        private void CaptureFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProfile = (this.CaptureFormatComboBox.SelectedItem as MediaWrapper.CaptureProfile);
            this.Wrapper.SetSelectedProfile(selectedProfile);
        }
    }
}
