using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml.Media.Imaging;


namespace ScanGallery
{
    /// <summary>
    /// Super simple interface for getting an image with a given brightness and contrast.
    /// Saves an original, so changes are non destructive.
    /// Caches a modified image to avoid recalculating requests for the same b and c.
    /// </summary>
    public class BitmapWrapper
    {
        private WriteableBitmap Original { get; }
        private WriteableBitmap Current { get; set; }

        private Brightness Brightness { get; }
        private Contrast Contrast { get; }

        public BitmapWrapper(WriteableBitmap bitmap)
        {
            this.Original = this.Current = bitmap;

            this.Brightness = new Brightness();
            this.Contrast = new Contrast();
        }

        public WriteableBitmap GetImage(Contrast contrast, Brightness brightness)
        {
            if (brightness.IsOriginal && contrast.IsOriginal)
            {
                return this.Original;
            }
            if (!this.Brightness.Equals(brightness) || !this.Contrast.Equals(contrast))
            {
                this.Contrast.SetCount(contrast.Count);
                this.Brightness.SetCount(brightness.Count);
                this.Current = CalculateImage(this.Original, this.Brightness.Value, this.Contrast.Value);
            }
            return this.Current;
        }

        /// <summary>
        /// Builds a new WriteableBitmap from the image parameter based on the
        /// brightness and contrast parameters.
        /// </summary>
        /// <param name="image"></param>
        /// <param name="brightness"></param>
        /// <param name="contrast"></param>
        /// <returns></returns>
        private static WriteableBitmap CalculateImage(WriteableBitmap image, 
            double brightness, double contrast, bool BlackAndWhite = true)
        {
            var destImage = new WriteableBitmap(image.PixelWidth, image.PixelHeight);
            var color = new byte[4];
            using (Stream source = image.PixelBuffer.AsStream())
            {
                using (Stream dest = destImage.PixelBuffer.AsStream())
                {
                    while (source.Read(color, 0, 4) > 0)
                    {
                        var alpha = color.Last() / (double)255;

                        // Save some time since all color channels are the same in black and white images
                        if (BlackAndWhite)
                        {
                            var value = AdjustColorChannel(color.First(), alpha, brightness, contrast);
                            color[0] = color[1] = color[2] = value;
                        }
                        // Else calculate each channel independently
                        else
                        {
                            for (int i = 0; i < 3; i++)
                            {
                                color[i] = AdjustColorChannel(color[i], alpha, brightness, contrast);
                            }
                        }
                        dest.Write(color, 0, 4);
                    }
                }
            }
            return destImage;
        }

        /// <summary>
        /// Calculates the adjusted color value based on the given color,
        /// alpha, brightness and contrast values
        /// </summary>
        /// <param name="color"></param>
        /// <param name="alpha"></param>
        /// <param name="brightness"></param>
        /// <param name="contrast"></param>
        /// <returns></returns>
        private static byte AdjustColorChannel(byte color, double alpha, 
            double brightness, double contrast)
        {
            var value = (double)color;

            value /= alpha;
            value = contrast * value + brightness;
            value *= alpha;

            return ClampToByte(value);
        }

        /// <summary>
        /// Clamps input double to a byte [0,255]
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static byte ClampToByte(double input)
        {
            if (input < 0)
                return 0;
            if (input > 255)
                return 255;
            return (byte)input;
        }
    }

}
