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
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;
using System.ComponentModel;
using ApolloLensLibrary.Imaging;
using ApolloLensLibrary.Utilities;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Networking;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.Media.SpeechRecognition;
using Windows.Media.Capture;
using Windows.ApplicationModel.Resources.Core;
using Windows.UI.Popups;
using Windows.System.Threading;



// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ScanGallery
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private IDicomStudy ImageCollection { get; set; }

        public IList<string> SeriesNames => this.ImageCollection?.GetSeriesNames();
        public SoftwareBitmapSource SoftwareBitmapSource { get; set; }

        private string ServerAddressKey { get; } = "CustomServerAddress";
        public string ServerAddress { get; set; }

        private SpeechRecognizer SpeechRecognizer { get; set; }

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            this.SoftwareBitmapSource = new SoftwareBitmapSource();

            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(width: 100, height: 100));

            if (!ApplicationData.Current.LocalSettings.Values.TryGetValue(this.ServerAddressKey, out object value))
            {
                value = "10.0.0.192";
                ApplicationData.Current.LocalSettings.Values["CustomServerAddress"] = (string)value;
            }
            this.ServerAddress = (string)value;
            this.OnPropertyChanged(nameof(this.ServerAddress));
        }

        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await this.InitializeRecognizer();
        }

        #region Images

        private async void LoadStudy_Click(object sender, RoutedEventArgs e)
        {
            this.LoadStudy.ToggleVisibility();
            this.ImageCollection = await DicomNetworking.GetStudyAsync(this.ServerAddress);
            this.OnStudyLoaded();
        }

        private async void LoadStudyLocal_Click(object sender, RoutedEventArgs e)
        {
            this.LoadStudyLocal.ToggleVisibility();
            this.ImageCollection = await DicomParser.GetStudy();
            this.OnStudyLoaded();
        }

        private void OnStudyLoaded()
        {
            this.ImageCollection.ImageChanged += async (s, smartBm) =>
            {
                await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                {
                    var bm = SoftwareBitmap.CreateCopyFromBuffer(
                        smartBm.GetImage().AsBuffer(),
                        BitmapPixelFormat.Bgra8,
                        smartBm.Width,
                        smartBm.Height,
                        BitmapAlphaMode.Premultiplied);

                    this.SoftwareBitmapSource = new SoftwareBitmapSource();
                    await this.SoftwareBitmapSource.SetBitmapAsync(bm);
                    this.OnPropertyChanged(nameof(this.SoftwareBitmapSource));
                });
            };

            this.LoadingScreen.ToggleVisibility();
            this.RunningScreen.ToggleVisibility();

            this.OnPropertyChanged(nameof(this.SeriesNames));
            this.SeriesSelect.SelectedIndex = 0;
            this.SetSlider();
        }

        private void SetSlider()
        {
            this.Slider.Minimum = 0;
            this.Slider.Maximum = this.ImageCollection.GetCurrentSeriesSize();
            this.Slider.Value = this.ImageCollection.GetCurrentIndex();
        }

        private void SeriesSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var series = (string)(sender as ComboBox).SelectedItem;
            this.ImageCollection.SetCurrentSeries(series);
            this.SetSlider();
        }

        private void BrightUp_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.IncreaseBrightness();
        }

        private void BrightDown_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.DecreaseBrightness();
        }

        private void ContrastUp_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.IncreaseContrast();
        }

        private void ContrastDown_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.DecreaseContrast();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.Reset();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            this.NextImage();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            this.PreviousImage();
        }

        private void NextImage()
        {
            this.Slider.Value += 1;
        }

        private void PreviousImage()
        {
            this.Slider.Value -= 1;
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var action = e.NewValue > e.OldValue ?
                new Func<bool>(this.ImageCollection.MoveNext) :
                new Func<bool>(this.ImageCollection.MovePrevious);

            var delta = (int)Math.Abs(e.NewValue - e.OldValue);
            foreach (var i in Util.Range(delta))
            {
                var success = action();
                if (!success)
                {
                    this.OnReachedLastImage();
                    break;
                }
            }
        }

        private void OnReachedLastImage()
        {
            this.StopScroll();
        }

        private void ServerAddressBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            ApplicationData.Current.LocalSettings.Values[this.ServerAddressKey] = textBox.Text;
        }

        #endregion


        #region Speech



        private async Task<bool> RequestMicrophonePermission()
        {
            try
            {
                // Request access to the microphone only, to limit the number of capabilities we need
                // to request in the package manifest.
                MediaCaptureInitializationSettings settings = new MediaCaptureInitializationSettings();
                settings.StreamingCaptureMode = StreamingCaptureMode.Audio;
                settings.MediaCategory = MediaCategory.Speech;
                MediaCapture capture = new MediaCapture();

                await capture.InitializeAsync(settings);
            }

            catch (UnauthorizedAccessException)
            {
                // The user has turned off access to the microphone. If this occurs, we should show an error, or disable
                // functionality within the app to ensure that further exceptions aren't generated when 
                // recognition is attempted.
                return false;
            }
            return true;
        }

        private async Task InitializeRecognizer()
        {
            var allowed = await this.RequestMicrophonePermission();
            if (!allowed)
                return;

            this.SpeechRecognizer = new SpeechRecognizer();

            //this.SpeechRecognizer.StateChanged += this.SpeechRecognizer_StateChanged;
            this.SpeechRecognizer.ContinuousRecognitionSession.ResultGenerated += this.ContinuousRecognitionSession_ResultGenerated;
            this.SpeechRecognizer.ContinuousRecognitionSession.Completed += this.ContinuousRecognitionSession_Completed;

            this.SpeechRecognizer.Constraints.Add(
                new SpeechRecognitionListConstraint(
                    new List<string>()
                    {
                        "next", "previous", "scroll left", "scroll right", "stop scrolling"
                    }));

            await this.SpeechRecognizer.CompileConstraintsAsync();
            await this.SpeechRecognizer.ContinuousRecognitionSession.StartAsync();
        }

        private async void ContinuousRecognitionSession_Completed(SpeechContinuousRecognitionSession sender, SpeechContinuousRecognitionCompletedEventArgs args)
        {
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                var message = new MessageDialog("Speech Recognition Closed.");
                await message.ShowAsync();
            });
        }

        private async void ContinuousRecognitionSession_ResultGenerated(
            SpeechContinuousRecognitionSession sender,
            SpeechContinuousRecognitionResultGeneratedEventArgs args)
        {
            var command = args.Result.Text;
            switch (command)
            {
                case "next":
                    await this.RunOnUi(this.NextImage);
                    break;
                case "previous":
                    await this.RunOnUi(this.PreviousImage);
                    break;
                case "scroll left":
                    await this.RunOnUi(() => this.StartScroll(true));
                    break;
                case "scroll right":
                    await this.RunOnUi(() => this.StartScroll(false));
                    break;
                case "stop scrolling":
                    await this.RunOnUi(this.StopScroll);
                    break;
            }
        }

        private ThreadPoolTimer Scroll;

        private void StartScroll(bool left)
        {
            if (this.Scroll != null)
                return;

            var op = left ?
                new Action(this.PreviousImage) :
                new Action(this.NextImage);
            var ts = TimeSpan.FromMilliseconds(100);
            this.Scroll = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
                {
                    await this.RunOnUi(op);
                },
                ts);
        }

        private void StopScroll()
        {
            if (this.Scroll == null)
                return;

            this.Scroll.Cancel();
            this.Scroll = null;
        }

        private async Task RunOnUi(Action action)
        {
            await this.Dispatcher.RunAsync(
                Windows.UI.Core.CoreDispatcherPriority.Normal,
                () => action());
        }
    }



    #endregion
}
