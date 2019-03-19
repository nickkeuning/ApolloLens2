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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ScanGallery
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private DicomManager dicom { get; set; }
        private IDicomStudy ImageCollection { get; set; }

        // Bound to MainPage.xaml
        public WriteableBitmap Image => this.ImageCollection.GetCurrentImage();
        public IList<string> Series => this.ImageCollection.GetSeriesNames();

        public MainPage()
        {
            this.DataContext = this;
            this.InitializeComponent();
            this.dicom = new DicomManager();
            this.ImageCollection = new ImageCollection();

            // Pass property changes in ImageCollection up to MainPage.xaml
            this.ImageCollection.PropertyChanged += (s, e) =>
            {
                // Translate ImageCollection method name to local property name
                var bindings = new Dictionary<string, string>()
                {
                    { "GetCurrentImage", "Image" },
                    { "GetSeriesNames", "Series" }
                };
                var name = bindings[e.PropertyName];
                this.OnPropertyChanged(name);
            };
        }

        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            var images = await this.dicom.GetImages();
            var mid = images.Count() / 2;
            this.ImageCollection.AddImagesToSeries(images.Take(mid), "First");
            this.ImageCollection.AddImagesToSeries(images.Skip(mid), "Second");
            this.SeriesSelect.SelectedIndex = 0;
        }

        private void BrightnessUp_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.IncreaseBrightness();
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

        private void BrightnessDown_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.DecreaseBrightness();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.MovePrevious();
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            this.ImageCollection.MoveNext();
        }

        private void SeriesSelect_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var seriesName = (string)this.SeriesSelect.SelectedItem;
            this.ImageCollection.SetCurrentSeries(seriesName);
        }
    }
}
