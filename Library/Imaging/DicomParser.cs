using Dicom;
using Dicom.Imaging;
using Dicom.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;


namespace ApolloLensLibrary.Imaging
{
    /// <summary>
    /// Responsible for loading a Dicom study from
    /// disk and returing it as an IImageCollection
    /// or raw dictionary of ImageTransferObjects.
    /// </summary>
    /// <remarks>
    /// FoDicom was needed primarily to parse dicom
    /// files to XML for data extraction, and to 
    /// render dicom image files to bitmaps.
    /// dicomFile.Dataset.WriteToXml() was used because
    /// the developer had more experience using Linq
    /// to XML than the FoDicom API.
    /// </remarks>
    public class DicomParser
    {
        #region Events

        public event EventHandler<int> ReadyToLoad;
        public event EventHandler LoadedImage;

        private void OnReadyToLoad(int numImages)
        {
            this.ReadyToLoad?.Invoke(this, numImages);
        }

        private void OnLoadedImage()
        {
            this.LoadedImage?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region Interface

        /// <summary>
        /// Wraps this.GetStudyRaw(), and builds an ImageCollection
        /// from the resulting collection.
        /// </summary>
        /// <returns></returns>
        public async Task<IImageCollection> GetStudyAsCollection()
        {
            var raw = await this.GetStudyRaw();

            var result = ImageCollection.Create();

            foreach (var series in raw.Keys)
            {
                result.CreateSeries(series, raw[series].Count());
                foreach (var im in raw[series])
                {
                    result.AddImageToSeries(im);
                }
            }

            return result;
        }

        /// <summary>
        /// Prompts user for a directory, then loads the
        /// Dicom study contained in that directory into
        /// memory.
        /// </summary>
        /// <returns></returns>
        public async Task<IDictionary<string, IEnumerable<ImageTransferObject>>> GetStudyRaw()
        {
            // prompt user for directory
            var directory = await this.GetDicomDirectory();

            // get the paths to each individual image dicom file
            var imagePaths = await this.GetImagePaths(directory);
            this.OnReadyToLoad(imagePaths.Count());

            var collection = new List<ImageTransferObject>();
            foreach (var path in imagePaths)
            {
                // load the specified path
                collection.Add(await this.ProcessImagePath(directory, path));
                this.OnLoadedImage();
            }

            // clean up collection, transform List<ImageTransferObject> into
            // IDictionary<string, IEnumerable<ImageTransferObject>>.
            // Also clean up the position field of each ImageTransferObject
            // to reflect the image's actual location in the list.
            var res = collection
                .GroupBy(transfer => transfer.Series)
                .ToDictionary(
                    grp => grp.Key,
                    grp =>
                    {
                        var ordered = grp
                            .OrderBy(transfer => transfer.Position);

                        foreach (var it in ordered
                            .Select((t, i) => new { value = t, idx = i }))
                        {
                            it.value.Position = it.idx;
                        }

                        return ordered.AsEnumerable();
                    });

            return res;
        }

        #endregion

        /// <summary>
        /// Prompt user for a directory and return the resulting
        /// storage folder async
        /// </summary>
        /// <returns></returns>
        private async Task<StorageFolder> GetDicomDirectory()
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            return await folderPicker.PickSingleFolderAsync();
        }

        /// <summary>
        /// Access and render the dicom image file at the specified
        /// path within the specified directory. Return as an
        /// ImageTransferObject async.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        private async Task<ImageTransferObject> ProcessImagePath(StorageFolder directory, string path)
        {
            var file = await directory.GetFileAsync(path);
            var stream = await file.OpenStreamForReadAsync();
            var dicomFile = await DicomFile.OpenAsync(stream);

            // dicom object seem easier to work with in xml
            var xdoc = XDocument.Parse(dicomFile.Dataset.WriteToXml());

            // access the images position
            var position = xdoc
                .GetElementsByDicomKeyword("InstanceNumber")
                .Select(elt => Convert.ToInt32(elt.Value))
                .FirstOrDefault();

            // access the image's series
            var series = xdoc
                .GetElementsByDicomKeyword("SeriesDescription")
                .Select(elt => elt.Value)
                .FirstOrDefault();

            // convert the image data itself into a byte array
            // of pixels
            var dicomImage = new DicomImage(dicomFile.Dataset);
            var pixelBuffer = dicomImage.RenderImage().AsWriteableBitmap().PixelBuffer;

            // need to copy pixel buffer out to new byte
            // array since pixel buffer will get destroyed.
            var reader = DataReader.FromBuffer(pixelBuffer);
            var imageBytes = new byte[pixelBuffer.Length];
            reader.ReadBytes(imageBytes);

            // bundle into an ImageTransferObject
            return new ImageTransferObject()
            {
                Image = imageBytes,
                Series = series,
                Position = position,
                Width = dicomImage.Width,
                Height = dicomImage.Height
            };
        }

        /// <summary>
        /// Get the file containing "dicomdir" (if there is one)
        /// from the specified directory. Returns null if the
        /// file doesn't exist.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        private async Task<string> GetDicomDirFileName(StorageFolder directory)
        {
            var files = await directory.GetFilesAsync();

            return files
                .Select(file => file.Name)
                .Where(name => name.ToLower().Contains("dicomdir"))
                .FirstOrDefault();
        }

        /// <summary>
        /// Gets a collection of paths representing every
        /// image in the study. If a "dicomdir" file is
        /// found, use this to determine paths. Otherwise,
        /// assume every path in the directory is an image.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        private async Task<IEnumerable<string>> GetImagePaths(StorageFolder directory)
        {
            var fileName = await this.GetDicomDirFileName(directory);
            if (fileName != null)
            {
                return await this.GetPathsFromDicomDirFile(directory, fileName);
            }
            return await this.GetPathsFromDirectory(directory);
        }

        /// <summary>
        /// Parses out a dicomdir file to xml, then
        /// returns the path of every image referenced
        /// in the study.
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private async Task<IEnumerable<string>> GetPathsFromDicomDirFile(StorageFolder directory, string fileName)
        {
            var dicomdirFile = await directory.GetFileAsync(fileName);
            var stream = await dicomdirFile.OpenStreamForReadAsync();
            var dicomFile = await DicomFile.OpenAsync(stream);

            var xdoc = XDocument.Parse(dicomFile.Dataset.WriteToXml());

            return xdoc
                .GetElementsByDicomKeyword("ReferencedFileID")
                .Select(elt =>
                {
                    var vals = elt
                        .Descendants()
                        .Select(val => val.Value);

                    return string.Join(@"\", vals);
                })
                .ToList();
        }

        /// <summary>
        /// Returns the path to every file in the directory.
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        private async Task<IEnumerable<string>> GetPathsFromDirectory(StorageFolder directory)
        {
            var files = await directory.GetFilesAsync();
            return files
                .Select(file => file.Name);
        }

    }

    public static class DicomXMLExtensions
    {
        /// <summary>
        /// Allows finding all xelements containing the
        /// specified keyword. Inspect result of
        /// dicomFile.Dataset.WriteToXml() in order to see
        /// structure of XML document.
        /// </summary>
        /// <param name="xdoc"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public static IEnumerable<XElement> GetElementsByDicomKeyword(this XDocument xdoc, string keyword)
        {
            return xdoc
                .Descendants("DicomAttribute")
                .Where(elt =>
                {
                    return elt
                        .Attribute("keyword")
                        .Value
                        .Contains(keyword);
                });
        }
    }
}
