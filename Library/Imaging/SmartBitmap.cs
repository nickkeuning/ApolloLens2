using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Storage.Streams;


namespace ApolloLensLibrary.Imaging
{
    /// <summary>
    /// Super simple interface for getting an image with
    /// a given brightness and contrast.
    /// Saves an original, so changes are non destructive.
    /// Caches a modified image to avoid recalculating 
    /// requests for the same brightness and contrast
    /// </summary>
    public class SmartBitmap
    {
        private byte[] OriginalBytes { get; }
        private byte[] CurrentBytes { get; set; }

        private static readonly object brightLock = new object();
        private static readonly object contrastLock = new object();
        private Brightness Brightness { get; }
        private Contrast Contrast { get; }

        public int Width { get; }
        public int Height { get; }

        /// <summary>
        /// Fired when an update to the brightness
        /// and / or contrast completes and each
        /// parameter is currently valid.
        /// </summary>
        public event EventHandler ImageUpdated;

        public SmartBitmap(byte[] image, int width, int height)
        {
            this.OriginalBytes = image;
            this.CurrentBytes = image;

            this.Width = width;
            this.Height = height;

            this.Brightness = new Brightness();
            this.Contrast = new Contrast();
        }

        public IBuffer GetImage()
        {
            return this.CurrentBytes.AsBuffer();
        }

        /// <summary>
        /// Pushes the work of recalculating the image for
        /// a new contrast and brightness to a background 
        /// thread with Task.Run(() => {});
        /// Only fires the event changed handler if the
        /// brightness and contrast calculated are still
        /// valid upon completion.
        /// </summary>
        /// <param name="contrast"></param>
        /// <param name="brightness"></param>
        public void AdjustImage(Contrast contrast, Brightness brightness)
        {
            if (!this.SetBrightnessAndContrast(contrast, brightness))
            {
                return;
            }

            var adjust = Task.Run(() =>
            {
                if (contrast.IsDefault && brightness.IsDefault)
                {
                    this.CurrentBytes = this.OriginalBytes;
                    this.ImageUpdated?.Invoke(this, EventArgs.Empty);
                    return;
                }

                var temp = new byte[this.OriginalBytes.Length];

                var pixel = new byte[4];
                using (Stream source = this.OriginalBytes.AsBuffer().AsStream())
                {
                    using (Stream dest = temp.AsBuffer().AsStream())
                    {
                        while (source.Read(pixel, 0, 4) > 0)
                        {
                            pixel = CalculatePixel(pixel, brightness.Value, contrast.Value);
                            dest.Write(pixel, 0, 4);
                        }
                    }
                }

                lock (brightLock)
                {
                    lock (contrastLock)
                    {
                        if (this.Brightness.Equals(brightness) && this.Contrast.Equals(contrast))
                        {
                            this.CurrentBytes = temp;
                            this.ImageUpdated?.Invoke(this, EventArgs.Empty);
                        }
                    }
                }
            });
        }

        private bool SetBrightnessAndContrast(Contrast contrast, Brightness brightness)
        {
            lock (brightLock)
            {
                lock (contrastLock)
                {
                    if (!this.Contrast.Equals(contrast) || !this.Brightness.Equals(brightness))
                    {
                        this.Contrast.SetTo(contrast);
                        this.Brightness.SetTo(brightness);
                        return true;
                    }
                    return false;
                }
            }
        }

        /// <summary>
        /// Calculate a pixel modified according to brightness and contrast
        /// </summary>
        /// <param name="pixel"></param>
        /// <param name="brightness"></param>
        /// <param name="contrast"></param>
        /// <param name="BlackAndWhite"></param>
        private static byte[] CalculatePixel(byte[] pixel, double brightness, double contrast, bool BlackAndWhite = true)
        {
            var alpha = pixel.Last() / (double)255;
            // Save some time since all color channels are the same in black and white images
            if (BlackAndWhite)
            {
                var value = CalculateColor(
                    pixel.First(),
                    alpha,
                    brightness,
                    contrast);
                pixel[0] = pixel[1] = pixel[2] = value;
            }
            // Else calculate each channel independently
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    pixel[i] = CalculateColor(
                        pixel[i],
                        alpha,
                        brightness,
                        contrast);
                }
            }
            return pixel;
        }

        /// <summary>
        /// Calculate a single color channel, modified by brightness and contrast
        /// </summary>
        /// <param name="color"></param>
        /// <param name="alpha"></param>
        /// <param name="brightness"></param>
        /// <param name="contrast"></param>
        /// <returns></returns>
        private static byte CalculateColor(
            byte color,
            double alpha,
            double brightness,
            double contrast)
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
