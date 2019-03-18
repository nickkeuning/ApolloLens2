using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Media.Imaging;
using System.ComponentModel;

namespace ScanGallery
{
    public class ImageCollection : IImageCollection
    {
        public ImageCollection()
        {
            this.Images = new List<BitmapWrapper>();
            this.Brightness = new Brightness();
            this.Contrast = new Contrast();
            this.CurrentIndex = 0;
        }

        public WriteableBitmap CurrentImage
        {
            get
            {
                return this.Images.ElementAtOrDefault(this.CurrentIndex)?.GetImage(this.Contrast, this.Brightness);
            }
        }

        int IImageCollection.Size
        {
            get
            {
                return this.Images.Count;
            }
        }

        private Brightness Brightness { get; }
        private Contrast Contrast { get; }
        private int CurrentIndex { get; set; }

        private List<BitmapWrapper> Images { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        public void AddImages(IEnumerable<WriteableBitmap> images)
        {
            this.Images.AddRange(images.Select(bm => new BitmapWrapper(bm)));
            this.OnProperyChanged(nameof(this.CurrentImage));
        }

        public void DecreaseBrightness()
        {
            this.Brightness.Decrease();
            this.OnProperyChanged(nameof(this.CurrentImage));
        }

        public void DecreaseContrast()
        {
            this.Contrast.Decrease();
            this.OnProperyChanged(nameof(this.CurrentImage));
        }

        public void IncreaseBrightness()
        {
            this.Brightness.Increase();
            this.OnProperyChanged(nameof(this.CurrentImage));
        }

        public void IncreaseContrast()
        {
            this.Contrast.Increase();
            this.OnProperyChanged(nameof(this.CurrentImage));
        }

        public void Reset()
        {
            this.Brightness.Reset();
            this.Contrast.Reset();
            this.OnProperyChanged(nameof(this.CurrentImage));
        }

        public void GoTo(int index)
        {
            if (index > 0 && index < this.Images.Count)
            {
                this.CurrentIndex = index;
            }
            this.OnProperyChanged(nameof(this.CurrentImage));
        }

        public void MoveNext()
        {
            if (this.CurrentIndex + 1 < this.Images.Count)
            {
                this.CurrentIndex++;
            }
            this.OnProperyChanged(nameof(this.CurrentImage));
        }

        public void MovePrevious()
        {
            if (this.CurrentIndex > 0)
            {
                this.CurrentIndex--;
            }
            this.OnProperyChanged(nameof(this.CurrentImage));
        }

        private void OnProperyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public interface IImageCollection : INotifyPropertyChanged
    {
        WriteableBitmap CurrentImage { get; }

        void AddImages(IEnumerable<WriteableBitmap> images);

        int Size { get; }

        void MovePrevious();
        void MoveNext();
        void GoTo(int index);

        void IncreaseContrast();
        void DecreaseContrast();
        void IncreaseBrightness();
        void DecreaseBrightness();
        void Reset();
    }
}
