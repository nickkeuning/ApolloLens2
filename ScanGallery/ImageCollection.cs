using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Media.Imaging;
using System.ComponentModel;

namespace ScanGallery
{
    //public class OldImageCollection : IDicomStudy
    //{
    //    public OldImageCollection()
    //    {
    //        this.Images = new List<SmartBitmap>();
    //        this.Brightness = new Brightness();
    //        this.Contrast = new Contrast();
    //        this.CurrentIndex = 0;
    //    }

    //    public WriteableBitmap GetCurrentImage
    //    {
    //        get
    //        {
    //            return this.Images.ElementAtOrDefault(this.CurrentIndex)?.GetImage(this.Contrast, this.Brightness);
    //        }
    //    }

    //    public int GetSize
    //    {
    //        get
    //        {
    //            return this.Images.Count;
    //        }
    //    }

    //    private Brightness Brightness { get; }
    //    private Contrast Contrast { get; }
    //    private int CurrentIndex { get; set; }

    //    private List<SmartBitmap> Images { get; }

    //    public event PropertyChangedEventHandler PropertyChanged;

    //    public void AddImagesToSeries(IEnumerable<WriteableBitmap> images)
    //    {
    //        this.Images.AddRange(images.Select(bm => new SmartBitmap(bm)));
    //        this.OnProperyChanged(nameof(this.GetCurrentImage));
    //    }

    //    public void DecreaseBrightness()
    //    {
    //        this.Brightness.Decrease();
    //        this.OnProperyChanged(nameof(this.GetCurrentImage));
    //    }

    //    public void DecreaseContrast()
    //    {
    //        this.Contrast.Decrease();
    //        this.OnProperyChanged(nameof(this.GetCurrentImage));
    //    }

    //    public void IncreaseBrightness()
    //    {
    //        this.Brightness.Increase();
    //        this.OnProperyChanged(nameof(this.GetCurrentImage));
    //    }

    //    public void IncreaseContrast()
    //    {
    //        this.Contrast.Increase();
    //        this.OnProperyChanged(nameof(this.GetCurrentImage));
    //    }

    //    public void Reset()
    //    {
    //        this.Brightness.Reset();
    //        this.Contrast.Reset();
    //        this.OnProperyChanged(nameof(this.GetCurrentImage));
    //    }

    //    public void GoTo(int index)
    //    {
    //        if (index > 0 && index < this.Images.Count)
    //        {
    //            this.CurrentIndex = index;
    //        }
    //        this.OnProperyChanged(nameof(this.GetCurrentImage));
    //    }

    //    public void MoveNext()
    //    {
    //        if (this.CurrentIndex + 1 < this.Images.Count)
    //        {
    //            this.CurrentIndex++;
    //        }
    //        this.OnProperyChanged(nameof(this.GetCurrentImage));
    //    }

    //    public void MovePrevious()
    //    {
    //        if (this.CurrentIndex > 0)
    //        {
    //            this.CurrentIndex--;
    //        }
    //        this.OnProperyChanged(nameof(this.GetCurrentImage));
    //    }

    //    private void OnProperyChanged(string propertyName)
    //    {
    //        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    //    }
    ////}

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

        public event PropertyChangedEventHandler PropertyChanged;

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

        private void OnPropertyChanged(string PropertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
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

        public WriteableBitmap GetCurrentImage()
        {
            this.Images.TryGetValue(this.CurrentSeries, out var series);
            var smartBitmap = series?.ElementAtOrDefault(this.CurrentIndex);
            return smartBitmap?.GetImage(this.Contrast, this.Brightness);
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
