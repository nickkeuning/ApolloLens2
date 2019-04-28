using ApolloLensLibrary.Utilities;
using ApolloLensLibrary.WebRtc;
//using WebRtcImplOld;
using WebRtcImplNew;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using System.Linq;

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

            var devices = await this.conductor.GetMediaDevices();
            this.MediaDeviceComboBox.ItemsSource = devices;
            this.MediaDeviceComboBox.SelectedIndex = 0;

            this.CaptureFormatComboBox.ItemsSource = 
                await this.conductor.GetCaptureProfiles(devices.First());
            this.CaptureFormatComboBox.SelectedIndex = 0;
        }

        private async void ShowVideo_Click(object sender, RoutedEventArgs e)
        {
            await this.conductor.StartCall();

            // set visibilities
            this.ShowVideo.Hide();
            this.CaptureFormatComboBox.Hide();
            this.MediaDeviceComboBox.Hide();
            this.LocalVideo.Show();
            this.HideVideo.Show();
        }

        private async void HideVideo_Click(object sender, RoutedEventArgs e)
        {
            await this.conductor.Shutdown();

            // set visibilites
            this.ShowVideo.Show();
            this.CaptureFormatComboBox.Show();
            this.MediaDeviceComboBox.Show();
            this.LocalVideo.Hide();
            this.HideVideo.Hide();
        }

        private void CaptureFormatComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedProfile = (this.CaptureFormatComboBox.SelectedItem as CaptureProfile);
            this.conductor.SetSelectedProfile(selectedProfile);
        }

        private async void MediaDeviceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var mediaDevice = (this.MediaDeviceComboBox.SelectedItem as MediaDevice);
            this.conductor.SetSelectedMediaDevice(mediaDevice);

            this.CaptureFormatComboBox.ItemsSource = 
                await this.conductor.GetCaptureProfiles(mediaDevice);
            this.CaptureFormatComboBox.SelectedIndex = 0;
        }
    }
}
