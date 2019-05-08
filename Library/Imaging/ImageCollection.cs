using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Storage.Streams;


namespace ApolloLensLibrary.Imaging
{
    /// <summary>
    /// Interface describing a collection of images
    /// with adjustable brightness and contrast
    /// </summary>
    /// <remarks>
    /// TODO: Bind brightness and contrast to each
    /// series independently instead of for the
    /// whole study.
    /// </remarks>
    public interface IImageCollection
    {
        /// <summary>
        /// Fired when any method that causes the
        /// "current image" to change completes.
        /// Includes changing which image is shown
        /// and changing the brightness and contrast
        /// of the collection.
        /// </summary>
        event EventHandler<ImageChangedEventArgs> ImageChanged;

        // Building
        void CreateSeries(string SeriesName, int SeriesSize);
        void AddImageToSeries(ImageTransferObject image);

        // Series info
        IEnumerable<string> GetSeriesNames();
        int GetCurrentSeriesSize();

        // Change series
        void SetCurrentSeries(string SeriesName);

        // Within series
        bool MoveNext();
        bool MovePrevious();

        // Image characteristics
        void IncreaseContrast();
        void DecreaseContrast();
        void IncreaseBrightness();
        void DecreaseBrightness();
        void Reset();
    }

    /// <summary>
    /// Implementation of ImageCollection interface.
    /// </summary>
    public class ImageCollection : IImageCollection
    {
        #region PrivateProperties

        /// <summary>
        /// Specify the brightness and contrast of the entire
        /// collection (study, in dicom terminology).
        /// </summary>
        /// <remarks>
        /// Should be changed so that each series within the
        /// study has its own brightness and contrast.
        /// </remarks>
        private Brightness Brightness { get; }
        private Contrast Contrast { get; }

        private int CurrentIndex { get; set; }
        private string CurrentSeries { get; set; }

        /// <summary>
        /// Primary data structure containing all actual image
        /// byte arrays.
        /// </summary>
        private Dictionary<string, SmartBitmap[]> Images { get; }

        /// <summary>
        /// Allows jumping back into a series at the same index
        /// the user left at.
        /// </summary>
        private Dictionary<string, int> SeriesIndexCache { get; }

        #endregion

        #region Creation

        /// <summary>
        /// Hide actual implementation / constructor.
        /// Not necessary. 
        /// </summary>
        /// <returns></returns>
        public static IImageCollection Create()
        {
            return new ImageCollection();
        }

        private ImageCollection()
        {
            this.Images = new Dictionary<string, SmartBitmap[]>();
            this.SeriesIndexCache = new Dictionary<string, int>();
            this.Brightness = new Brightness();
            this.Contrast = new Contrast();
            this.CurrentIndex = 0;
            this.CurrentSeries = "";
        }

        #endregion

        #region IImageCollection

        /// <summary>
        /// Fired when either
        ///     A) which image to be displayed changes
        ///     B) the brightness and / or contrast of the
        ///         collection is changed, and the current image
        ///         gets done recalculating
        /// Used to limit expensivd UI updates
        /// </summary>
        public event EventHandler<ImageChangedEventArgs> ImageChanged;

        /// <summary>
        /// Build ImageChangedEventArgs object and invokes
        /// image changed handler.
        /// </summary>
        private void onImageChanged()
        {
            var current = this.getCurrentImage();
            var args = new ImageChangedEventArgs()
            {
                Image = current.GetImage(),
                Width = current.Width,
                Height = current.Height,
                Index = this.CurrentIndex
            };
            this.ImageChanged?.Invoke(this, args);
        }

        /// <summary>
        /// Add a series of the specified name and size to
        /// the collection.
        /// Uses raw array to minimize overhead.
        /// </summary>
        /// <param name="seriesName"></param>
        /// <param name="seriesSize"></param>
        public void CreateSeries(string seriesName, int seriesSize)
        {
            if (!this.Images.ContainsKey(seriesName))
            {
                this.Images.Add(seriesName, new SmartBitmap[seriesSize]);
                this.SeriesIndexCache.Add(seriesName, 0);
            }
        }

        /// <summary>
        /// Add image, described by transfer object, to the collection.
        /// </summary>
        /// <param name="image"></param>
        public void AddImageToSeries(ImageTransferObject image)
        {
            if (!this.Images.ContainsKey(image.Series))
                throw new ArgumentException();
            if (image.Position < 0 || image.Position >= this.Images[image.Series].Count())
                throw new ArgumentException();

            var smartBitmap = new SmartBitmap(image.Image, image.Width, image.Height);
            smartBitmap.ImageUpdated += (s, e) =>
            {
                if (image.Series == this.CurrentSeries 
                    && image.Position == this.CurrentIndex)
                {
                    this.onImageChanged();
                }
            };

            this.Images[image.Series][image.Position] = smartBitmap;
        }

        public IEnumerable<string> GetSeriesNames()
        {
            return this.Images.Keys.AsEnumerable();
        }

        public int GetCurrentSeriesSize()
        {
            return this.getCurrentSeries().Count;
        }

        public void SetCurrentSeries(string SeriesName)
        {
            if (this.Images.ContainsKey(SeriesName) && this.CurrentSeries != SeriesName)
            {
                this.SeriesIndexCache[this.CurrentSeries] = this.CurrentIndex;
                this.CurrentIndex = this.SeriesIndexCache[SeriesName];
                this.CurrentSeries = SeriesName;
                this.setImageCharacteristics();
                this.onImageChanged();
            }
        }

        public bool MoveNext()
        {
            if (this.CurrentIndex + 1 == this.GetCurrentSeriesSize())
                return false;

            this.CurrentIndex++;
            this.onImageChanged();

            // adjust the image two away so that image will be up to date
            // upon navigating to it.
            this.getNextImage(2)?.AdjustImage(this.Contrast, this.Brightness);
            return true;
        }

        public bool MovePrevious()
        {
            if (this.CurrentIndex == 0)
                return false;

            this.CurrentIndex--;
            this.onImageChanged();

            // adjust the image two away so that image will be up to date
            // upon navigating to it.
            this.getPreviousImage(2)?.AdjustImage(this.Contrast, this.Brightness);
            return true;
        }

        public void DecreaseBrightness()
        {
            this.Brightness.Decrease();
            this.setImageCharacteristics();
        }

        public void DecreaseContrast()
        {
            this.Contrast.Decrease();
            this.setImageCharacteristics();
        }

        public void IncreaseBrightness()
        {
            this.Brightness.Increase();
            this.setImageCharacteristics();
        }

        public void IncreaseContrast()
        {
            this.Contrast.Increase();
            this.setImageCharacteristics();
        }

        public void Reset()
        {
            this.Brightness.Reset();
            this.Contrast.Reset();
            this.setImageCharacteristics();
        }

        #endregion

        #region PrivateMethods

        private void setImageCharacteristics()
        {
            // Adjust 5 images total
                // The current image
                // Two neighbors right
                // two neighbors left
            // Improves scrolling smoothness on the UI
            this.getCurrentImage()?.AdjustImage(this.Contrast, this.Brightness);
            this.getPreviousImage(2)?.AdjustImage(this.Contrast, this.Brightness);
            this.getPreviousImage(1)?.AdjustImage(this.Contrast, this.Brightness);
            this.getNextImage(1)?.AdjustImage(this.Contrast, this.Brightness);
            this.getNextImage(2)?.AdjustImage(this.Contrast, this.Brightness);
        }

        private IList<SmartBitmap> getCurrentSeries()
        {
            this.Images.TryGetValue(this.CurrentSeries, out var series);
            return series;
        }

        // NOTE: these will return null upon failure
        private SmartBitmap getCurrentImage()
        {
            return this.getCurrentSeries()?.ElementAtOrDefault(this.CurrentIndex);
        }

        private SmartBitmap getNextImage(int offset)
        {
            return this.getCurrentSeries()?.ElementAtOrDefault(this.CurrentIndex + offset);
        }

        private SmartBitmap getPreviousImage(int offset)
        {
            return this.getCurrentSeries()?.ElementAtOrDefault(this.CurrentIndex - offset);
        }        

        #endregion
    }

    public class ImageChangedEventArgs : EventArgs
    {
        public IBuffer Image;
        public int Width;
        public int Height;
        public int Index;
    }
}
