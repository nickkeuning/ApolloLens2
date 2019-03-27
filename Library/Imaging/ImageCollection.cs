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
            this.ImageChanged?.Invoke(this, this.CurrentImage());
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
            this.CurrentImage().AdjustImage(this.Contrast, this.Brightness);
            this.NextImage()?.AdjustImage(this.Contrast, this.Brightness);
            this.PreviousImage()?.AdjustImage(this.Contrast, this.Brightness);
        }

        private IList<SmartBitmap> GetCurrentSeries()
        {
            this.Images.TryGetValue(this.CurrentSeries, out var series);
            return series;
        }

        private SmartBitmap CurrentImage()
        {
            return this.GetCurrentSeries()?.ElementAtOrDefault(this.CurrentIndex);
        }

        private SmartBitmap NextImage()
        {
            return this.GetCurrentSeries()?.ElementAtOrDefault(this.CurrentIndex + 1);
        }

        private SmartBitmap PreviousImage()
        {
            return this.GetCurrentSeries()?.ElementAtOrDefault(this.CurrentIndex - 1);
        }

        public IList<string> GetSeriesNames()
        {
            return this.Images.Keys.ToList();
        }

        public bool MoveNext()
        {
            if (this.CurrentIndex + 1 < this.GetCurrentSeriesSize())
            {
                this.CurrentIndex++;
                this.NextImage()?.AdjustImage(this.Contrast, this.Brightness);
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
                this.PreviousImage()?.AdjustImage(this.Contrast, this.Brightness);
                this.OnImageChanged();
                return true;
            }
            return false;
        }

        public void SetCurrentSeries(string SeriesName)
        {
            if (this.Images.ContainsKey(SeriesName) && this.CurrentSeries != SeriesName)
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

        public int GetCurrentSeriesSize()
        {
            return this.GetCurrentSeries().Count;
        }

        public int GetCurrentIndex()
        {
            return this.CurrentIndex;
        }
    }

    public interface IDicomStudy
    {
        event EventHandler<SmartBitmap> ImageChanged;

        // Series
        IList<string> GetSeriesNames();
        void CreateSeries(string SeriesName, int SeriesSize);
        void AddImageToSeries(byte[] image, string seriesName, int position, int width, int height);
        void SetCurrentSeries(string SeriesName);
        int GetCurrentSeriesSize();
        int GetCurrentIndex();

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
