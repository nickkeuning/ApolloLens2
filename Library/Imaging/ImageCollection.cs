using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Media.Imaging;
using System.ComponentModel;
using System;
using Windows.Storage;
using Windows.Storage.Streams;
using System.IO;
using Windows.Graphics.Imaging;


namespace ApolloLensLibrary.Imaging
{
    public class ImageCollection : IDicomStudy
    {
        private Brightness Brightness { get; }
        private Contrast Contrast { get; }

        private int CurrentIndex { get; set; }
        private string CurrentSeries { get; set; }

        private Dictionary<string, SmartBitmap[]> Images { get; }
        private Dictionary<string, int> SeriesIndexCache { get; }

        public ImageCollection()
        {
            this.Images = new Dictionary<string, SmartBitmap[]>();
            this.SeriesIndexCache = new Dictionary<string, int>();
            this.Brightness = new Brightness();
            this.Contrast = new Contrast();
            this.CurrentIndex = 0;
            this.CurrentSeries = "";
        }

        public event EventHandler<SmartBitmap> ImageChanged;

        private void OnImageChanged()
        {
            this.ImageChanged?.Invoke(this, this.GetCurrentSmartBitmap());
        }

        public void DecreaseBrightness()
        {
            this.Brightness.Decrease();
            this.SetImageCharacteristics();
        }

        public void DecreaseContrast()
        {
            this.Contrast.Decrease();
            this.SetImageCharacteristics();
        }

        public void IncreaseBrightness()
        {
            this.Brightness.Increase();
            this.SetImageCharacteristics();
        }

        public void IncreaseContrast()
        {
            this.Contrast.Increase();
            this.SetImageCharacteristics();
        }

        public void Reset()
        {
            this.Brightness.Reset();
            this.Contrast.Reset();
            this.SetImageCharacteristics();
        }

        private void SetImageCharacteristics()
        {
            this.GetCurrentSmartBitmap().AdjustImage(this.Contrast, this.Brightness);
        }

        private SmartBitmap GetCurrentSmartBitmap()
        {
            this.Images.TryGetValue(this.CurrentSeries, out var series);
            return series?.ElementAtOrDefault(this.CurrentIndex);
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
            return this.Images[SeriesName].Count();
        }

        public bool MoveNext()
        {
            var currentSeriesSize = this.GetSeriesSize(this.CurrentSeries);
            if (this.CurrentIndex + 1 < currentSeriesSize)
            {
                this.CurrentIndex++;
                this.SetImageCharacteristics();
                this.OnImageChanged();
                return true;
            }
            return false;
        }

        public bool MovePrevious()
        {
            if (this.CurrentIndex > 0)
            {
                this.CurrentIndex--;
                this.SetImageCharacteristics();
                this.OnImageChanged();
                return true;
            }
            return false;
        }

        public void SetCurrentSeries(string SeriesName)
        {
            if (this.Images.ContainsKey(SeriesName))
            {
                this.SeriesIndexCache[this.CurrentSeries] = this.CurrentIndex;
                this.CurrentIndex = this.SeriesIndexCache[SeriesName];
                this.CurrentSeries = SeriesName;
                this.SetImageCharacteristics();
                this.OnImageChanged();
            }
        }

        public void CreateSeries(string seriesName, int seriesSize)
        {
            if (!this.Images.ContainsKey(seriesName))
            {
                this.Images.Add(seriesName, new SmartBitmap[seriesSize]);
                this.SeriesIndexCache.Add(seriesName, 0);
            }
        }

        public void AddImageToSeries(byte[] image, string seriesName, int position, int width, int height)
        {
            if (!this.Images.ContainsKey(seriesName))
                throw new ArgumentException();
            if (position < 0 || position >= this.Images[seriesName].Count())
                throw new ArgumentException();

            var smartBitmap = new SmartBitmap(image, width, height);
            smartBitmap.ImageUpdated += (s, e) =>
            {
                if (seriesName == this.CurrentSeries && position == this.CurrentIndex)
                {
                    this.OnImageChanged();
                }
            };

            this.Images[seriesName][position] = smartBitmap;
        }
    }

    public interface IDicomStudy
    {
        //byte[] GetCurrentImage();
        event EventHandler<SmartBitmap> ImageChanged;

        // Series
        IList<string> GetSeriesNames();
        void CreateSeries(string SeriesName, int SeriesSize);
        void AddImageToSeries(byte[] image, string seriesName, int position, int width, int height);
        void SetCurrentSeries(string SeriesName);
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
