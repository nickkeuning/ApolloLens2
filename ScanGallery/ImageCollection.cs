﻿using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Media.Imaging;
using System.ComponentModel;

namespace ScanGallery
{
    public class ImageCollection : IDicomStudy
    {
        private Brightness Brightness { get; }
        private Contrast Contrast { get; }

        private int CurrentIndex { get; set; }
        private string CurrentSeries { get; set; }

        private Dictionary<string, List<SmartBitmap>> Images { get; }
        private Dictionary<string, int> SeriesIndexCache { get; }

        public ImageCollection()
        {
            this.Images = new Dictionary<string, List<SmartBitmap>>();
            this.SeriesIndexCache = new Dictionary<string, int>();
            this.Brightness = new Brightness();
            this.Contrast = new Contrast();
            this.CurrentIndex = 0;
            this.CurrentSeries = "";
        }

        private void OnPropertyChanged(string PropertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public WriteableBitmap GetCurrentImage()
        {
            this.Images.TryGetValue(this.CurrentSeries, out var series);
            var smartBitmap = series?.ElementAtOrDefault(this.CurrentIndex);
            return smartBitmap?.GetImage(this.Contrast, this.Brightness);
        }

        public void AddImagesToSeries(IEnumerable<WriteableBitmap> images, string SeriesName)
        {
            if (!images.Any())
                return;

            if (!this.Images.ContainsKey(SeriesName))
            {
                this.Images.Add(SeriesName, new List<SmartBitmap>());
                this.SeriesIndexCache.Add(SeriesName, 0);
            }

            this.Images[SeriesName].AddRange(images.Select(bm => new SmartBitmap(bm)));
            this.OnPropertyChanged(nameof(this.GetSeriesNames));
        }

        public void DecreaseBrightness()
        {
            this.Brightness.Decrease();
            this.OnPropertyChanged(nameof(this.GetCurrentImage));
        }

        public void DecreaseContrast()
        {
            this.Contrast.Decrease();
            this.OnPropertyChanged(nameof(this.GetCurrentImage));
        }

        public void IncreaseBrightness()
        {
            this.Brightness.Increase();
            this.OnPropertyChanged(nameof(this.GetCurrentImage));
        }

        public void IncreaseContrast()
        {
            this.Contrast.Increase();
            this.OnPropertyChanged(nameof(this.GetCurrentImage));
        }

        public void Reset()
        {
            this.Brightness.Reset();
            this.Contrast.Reset();
            this.OnPropertyChanged(nameof(this.GetCurrentImage));
        }

        public string GetCurrentSeries()
        {
            return this.CurrentSeries;
        }

        public IList<string> GetSeriesNames()
        {
            return this.Images.Keys.ToList();
        }

        public int GetSeriesSize(string SeriesName)
        {
            if (!this.Images.ContainsKey(SeriesName))
            {
                throw new System.ArgumentException("Series not contained in collection.");
            }
            return this.Images[SeriesName].Count;
        }

        public bool MoveNext()
        {
            var currentSeriesSize = this.GetSeriesSize(this.CurrentSeries);
            if (this.CurrentIndex + 1 < currentSeriesSize)
            {
                this.CurrentIndex++;
                this.OnPropertyChanged(nameof(this.GetCurrentImage));
                return true;
            }
            return false;
        }

        public bool MovePrevious()
        {
            if (this.CurrentIndex > 0)
            {
                this.CurrentIndex--;
                this.OnPropertyChanged(nameof(this.GetCurrentImage));
                return true;
            }
            return false;
        }

        public bool SetCurrentSeries(string SeriesName)
        {
            if (this.Images.ContainsKey(SeriesName))
            {
                this.SeriesIndexCache[this.CurrentSeries] = this.CurrentIndex;
                this.CurrentIndex = this.SeriesIndexCache[SeriesName];
                this.CurrentSeries = SeriesName;
                this.OnPropertyChanged(nameof(this.GetCurrentImage));
                return true;
            }
            return false;
        }
    }

    public interface IDicomStudy : INotifyPropertyChanged
    {
        WriteableBitmap GetCurrentImage();

        // Series
        IList<string> GetSeriesNames();
        void AddImagesToSeries(IEnumerable<WriteableBitmap> images, string SeriesName);
        bool SetCurrentSeries(string SeriesName);
        int GetSeriesSize(string SeriesName);
        string GetCurrentSeries();

        // Within series
        bool MovePrevious();
        bool MoveNext();

        // Image characteristics
        void IncreaseContrast();
        void DecreaseContrast();
        void IncreaseBrightness();
        void DecreaseBrightness();
        void Reset();
    }
}
