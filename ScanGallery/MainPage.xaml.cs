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

        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;



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
            ApplicationView.GetForCurrentView().SetPreferredMinSize(
                new Size(width: 1, height: 1));

            this.ImageCollection.ImageChanged += this.ImageCollection_ImageChanged;
            this.ImageCollection.ImageChanged += this.ImageCollection_ImageChangedResize;

            this.LoadingScreen.ToggleVisibility();
            this.RunningScreen.ToggleVisibility();

            this.OnPropertyChanged(nameof(this.SeriesNames));
            this.SeriesSelect.SelectedIndex = 0;
            this.SetSlider();
        }

        private void ImageCollection_ImageChangedResize(object sender, SmartBitmap smartBm)
        {
            var res = ApplicationView.GetForCurrentView().TryResizeView(
                new Size(width: smartBm.Width, height: smartBm.Height));
            this.ImageCollection.ImageChanged -= this.ImageCollection_ImageChangedResize;
        }

        private async void ImageCollection_ImageChanged(object sender, SmartBitmap smartBm)
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
        }

        private void SetSlider()
        {
            this.Slider.Minimum = 0;
            this.Slider.Maximum = this.ImageCollection.GetCurrentSeriesSize();
            this.Slider.Value = this.ImageCollection.GetCurrentIndex();
        }

        private void SeriesSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.ImageCollection.ImageChanged += this.ImageCollection_ImageChangedResize;
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
            this.Slider.Value += 1;
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            this.Slider.Value -= 1;
        }

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var action = e.NewValue > e.OldValue ?
                new Action(this.ImageCollection.MoveNext) :
                new Action(this.ImageCollection.MovePrevious);

            var delta = (int)Math.Abs(e.NewValue - e.OldValue);
            foreach (var i in Util.Range(delta))
            {
                action();
            }
        }

        private void ServerAddressBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            var textBox = (TextBox)sender;
            ApplicationData.Current.LocalSettings.Values[this.ServerAddressKey] = textBox.Text;
        }
    }
}
