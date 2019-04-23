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
using Windows.UI.Core;



// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ScanGallery
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private IImageCollection ImageCollection { get; set; }

        #region Bindings

        public Array Stretches { get; } = Enum.GetNames(typeof(Stretch));

        public IEnumerable<string> SeriesNamesItems => 
            this.ImageCollection?.GetSeriesNames().Select((s, i) => $"{i + 1}. {s}");

        public SoftwareBitmapSource SoftwareBitmapSource { get; set; }

        private string ServerAddressKey { get; } = "CustomServerAddress";
        public string ServerAddress { get; set; }

        private int index;
        public int Index
        {
            get
            {
                return this.index;
            }
            set
            {
                var moveImage = index < value ?
                    new Action(() => this.ImageCollection.MoveNext()) :
                    new Action(() => this.ImageCollection.MovePrevious());

                foreach (var i in Util.Range(Math.Abs(index - value)))
                {
                    moveImage();
                }
            }
        }

        #endregion

        #region Startup

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            this.SoftwareBitmapSource = new SoftwareBitmapSource();

            if (!ApplicationData.Current.LocalSettings.Values.TryGetValue(this.ServerAddressKey, out object value))
            {
                value = "10.0.0.192";
                ApplicationData.Current.LocalSettings.Values["CustomServerAddress"] = (string)value;
            }
            this.ServerAddress = (string)value;
            this.OnPropertyChanged(nameof(this.ServerAddress));
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            await this.InitializeRecognizer();
        }

        #endregion

        #region INotify

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region LoadingUIHandlers

        private async void LoadStudy_Click(object sender, RoutedEventArgs e)
        {
            this.LoadSettings.Hide();

            var loader = new DicomNetworking();
            loader.ReadyToLoad += (s, num) =>
            {
                this.LoadBarPanel.Show();

                this.LoadingBar.Maximum = num;
            };

            loader.LoadedImage += (s, a) =>
            {
                this.LoadingBar.Value++;
            };

            this.ImageCollection = await loader.GetStudyAsync(this.ServerAddress);
            await this.OnStudyLoaded();
        }

        private async void LoadStudyLocal_Click(object sender, RoutedEventArgs e)
        {
            this.LoadSettings.Hide();

            var parser = new DicomParser();
            parser.ReadyToLoad += async (s, num) =>
            {
                await this.RunOnUi(() =>
                {
                    this.LoadBarPanel.Show();

                    this.LoadingBar.Maximum = num;
                });
            };

            parser.LoadedImage += async (s, a) =>
            {
                await this.RunOnUi(() =>
                {
                    this.LoadingBar.Value++;
                });
            };

            this.ImageCollection = await parser.GetStudyAsCollection();
            await this.OnStudyLoaded();
        }

        private async Task OnStudyLoaded()
        {
            this.ImageCollection.ImageChanged += async (s, args) =>
            {
                await this.RunOnUi(async () =>
                {
                    this.index = args.Index;
                    this.OnPropertyChanged(nameof(this.Index));

                    var bm = SoftwareBitmap.CreateCopyFromBuffer(
                        args.Image,
                        BitmapPixelFormat.Bgra8,
                        args.Width,
                        args.Height,
                        BitmapAlphaMode.Premultiplied);

                    this.SoftwareBitmapSource = new SoftwareBitmapSource();
                    await this.SoftwareBitmapSource.SetBitmapAsync(bm);
                    this.OnPropertyChanged(nameof(this.SoftwareBitmapSource));
                });
            };

            this.LoadingScreen.Hide();
            this.RunningScreen.Show();

            this.OnPropertyChanged(nameof(this.SeriesNamesItems));
            this.SeriesSelect.SelectedIndex = 0;
            this.StretchSelect.SelectedIndex = 2;

            await this.StartListening();
        }

        #endregion

        #region UIHandlers

        private void StretchSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var str = (string)this.StretchSelect.SelectedItem;
            Enum.TryParse(typeof(Stretch), str, out object stretch);
            this.Image.Stretch = (Stretch)stretch;
        }

        private void SeriesSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var series = this.ImageCollection
                .GetSeriesNames()
                .ElementAt(this.SeriesSelect.SelectedIndex);

            this.ImageCollection.SetCurrentSeries(series);
            this.Slider.Maximum = this.ImageCollection.GetCurrentSeriesSize() - 1;
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
            this.ImageCollection.MoveNext();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.MovePrevious();
        }

        private void ServerAddressBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            ApplicationData.Current.LocalSettings.Values[this.ServerAddressKey] = textBox.Text;
        }

        #endregion

        #region Speech

        private SpeechRecognizer SpeechRecognizer { get; set; }
        private ThreadPoolTimer Scroll { get; set; }

        private async Task<bool> RequestMicrophonePermission()
        {
            try
            {
                var settings = new MediaCaptureInitializationSettings();
                settings.StreamingCaptureMode = StreamingCaptureMode.Audio;
                settings.MediaCategory = MediaCategory.Speech;
                var capture = new MediaCapture();

                await capture.InitializeAsync(settings);
            }
            catch (UnauthorizedAccessException)
            {
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

            this.SpeechRecognizer.Constraints.Add(
                new SpeechRecognitionListConstraint(
                    new List<string>()
                    {
                        "next",
                        "previous",
                        "scroll left",
                        "scroll right",
                        "stop scrolling",
                        "switch series",
                        "select series"
                    }));            
        }

        private async Task StartListening()
        {
            this.AddSeriesCommands();
            await this.SpeechRecognizer.CompileConstraintsAsync();
            await this.StartContinuousRecognition();
        }

        private void AddSeriesCommands()
        {
            var nums = Util.Range(this.ImageCollection.GetSeriesNames().Count());
            var commands = nums
                .Select(num => $"series {num + 1}")
                .ToList();

            this.SpeechRecognizer.Constraints.Add(
                new SpeechRecognitionListConstraint(commands));

            this.SpeechRecognizer.ContinuousRecognitionSession.ResultGenerated += async (s, args) =>
            {
                await this.RunOnUi(() =>
                {
                    if (!this.SeriesSelect.IsDropDownOpen || !commands.Contains(args.Result.Text))
                        return;

                    var rawCommand = args.Result.Text.Split(' ').ElementAtOrDefault(1);
                    if (int.TryParse(rawCommand, out var index))
                    {
                        this.SeriesSelect.SelectedIndex = index - 1;
                        this.SeriesSelect.IsDropDownOpen = false;
                    }
                });

            };
        }

        private async Task StartContinuousRecognition()
        {
            var continuous = this.SpeechRecognizer.ContinuousRecognitionSession;

            continuous.Completed += async (s, a) =>
            {
                await this.RunOnUi(async () =>
                {
                    var message = new MessageDialog("Speech Recognition Closed.");
                    await message.ShowAsync();
                });
            };

            continuous.ResultGenerated += async (s, args) =>
            {
                var command = args.Result.Text;
                switch (command)
                {
                    case "next":
                        await this.RunOnUi(() => this.ImageCollection.MoveNext());
                        break;
                    case "previous":
                        await this.RunOnUi(() => this.ImageCollection.MovePrevious());
                        break;
                    case "scroll left":
                        await this.RunOnUi(() => this.StartScroll(true));
                        break;
                    case "scroll right":
                        await this.RunOnUi(() => this.StartScroll(false));
                        break;
                    case "stop scrolling":
                        await this.RunOnUi(() => this.StopScroll());
                        break;
                    case "select series":
                    case "switch series":
                        await this.RunOnUi(() => this.OpenSeriesSelect());
                        break;
                }
            };

            await this.SpeechRecognizer.ContinuousRecognitionSession.StartAsync();
        }


        private void StartScroll(bool left)
        {
            if (this.Scroll != null)
                return;

            var moveImage = left ?
                new Func<bool>(this.ImageCollection.MovePrevious) :
                new Func<bool>(this.ImageCollection.MoveNext);

            var ts = TimeSpan.FromMilliseconds(100);
            this.Scroll = ThreadPoolTimer.CreatePeriodicTimer(async (source) =>
                {
                    await this.RunOnUi(() =>
                    {
                        if (!moveImage())
                            this.StopScroll();
                    });
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

        private void OpenSeriesSelect()
        {
            this.SeriesSelect.IsDropDownOpen = true;
        }

        #endregion

        #region Utility

        private async Task RunOnUi(Action action)
        {
            await this.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal, () => action());
        }

        #endregion

    }
}
