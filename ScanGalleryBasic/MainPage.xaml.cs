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
using ApolloLensLibrary.Imaging;
using System.ComponentModel;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;
using Windows.Graphics.Imaging;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ScanGalleryBasic
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {

        private IDicomStudy ImageCollection { get; set; }

        public IList<string> SeriesNames => this.ImageCollection?.GetSeriesNames();
        public SoftwareBitmapSource SoftwareBitmapSource { get; set; }

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            this.SoftwareBitmapSource = new SoftwareBitmapSource();
        }

        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.ImageCollection = await DicomManager.GetStudy();
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

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            this.Slider.Value += 1;
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            this.Slider.Value -= 1;
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

        private void Slider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (e.NewValue > e.OldValue)
            {
                this.ImageCollection.MoveNext();
            }
            else
            {
                this.ImageCollection.MovePrevious();
            }
        }
    }
}
