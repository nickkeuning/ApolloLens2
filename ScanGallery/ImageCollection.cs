using System.Collections.Generic;
using System.Linq;
using Windows.UI.Xaml.Media.Imaging;

namespace ScanGallery
{
    public class ImageCollection : IImageCollection
    {
        public WriteableBitmap Current => this.Images.ElementAtOrDefault(this.CurrentIndex)?.GetImage(this.Contrast, this.Brightness);
        int IImageCollection.Size => this.Images.Count;

        public ImageCollection()
        {
            this.Images = new List<BitmapWrapper>();
            this.Brightness = new Brightness();
            this.Contrast = new Contrast();
            this.CurrentIndex = 0;
        }

        private Brightness Brightness { get; }
        private Contrast Contrast { get; }
        private int CurrentIndex { get; set; }

        private List<BitmapWrapper> Images { get; }

        public void AddImages(IEnumerable<WriteableBitmap> images)
        {
            this.Images.AddRange(images.Select(bm => new BitmapWrapper(bm)));
        }

        public void DecreaseBrightness()
        {
            this.Brightness.Decrease();
        }

        public void DecreaseContrast()
        {
            this.Contrast.Decrease();
        }

        public void IncreaseBrightness()
        {
            this.Brightness.Increase();
        }

        public void IncreaseContrast()
        {
            this.Contrast.Increase();
        }

        public void Reset()
        {
            this.Brightness.Reset();
            this.Contrast.Reset();
        }

        public void GoTo(int index)
        {
            if (index > 0 && index < this.Images.Count)
            {
                this.CurrentIndex = index;
            }
        }

        public void MoveNext()
        {
            if (this.CurrentIndex + 1 < this.Images.Count)
            {
                this.CurrentIndex++;
            }
        }

        public void MovePrevious()
        {
            if (this.CurrentIndex > 0)
            {
                this.CurrentIndex--;
            }
        }
    }

    public interface IImageCollection
    {
        WriteableBitmap Current { get; }

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
