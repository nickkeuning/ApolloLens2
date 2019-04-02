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
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Imaging;
using Dicom.Imaging.Render;



namespace ApolloLensLibrary.Imaging
{
    public class DicomParser
    {
        public static async Task<IDicomStudy> GetStudy()
        {
            var raw = await GetStudyRaw();

            var result = new ImageCollection();

            foreach (var series in raw.Keys)
            {
                result.CreateSeries(series, raw[series].Count());
                foreach (var im in raw[series])
                {
                    result.AddImageToSeries(
                        im.Image, im.Series, im.Position,
                        im.Width, im.Height);
                }
            }

            return result;
        }

        public static async Task<IDictionary<string, IEnumerable<ImageTransferObject>>> GetStudyRaw()
        {
            var directory = await GetDicomDirectory();
            var imagePaths = await directory.GetImagePaths();

            var collection = new List<ImageTransferObject>(imagePaths.Count());
            foreach (var path in imagePaths)
            {
                collection.Add(await ProcessImagePath(directory, path));
            }

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

        private static async Task<StorageFolder> GetDicomDirectory()
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            return await folderPicker.PickSingleFolderAsync();
        }

        private static async Task<ImageTransferObject> ProcessImagePath(StorageFolder directory, string path)
        {
            var file = await directory.GetFileAsync(path);
            var stream = await file.OpenStreamForReadAsync();
            var dicomFile = await DicomFile.OpenAsync(stream);

            var xdoc = XDocument.Parse(dicomFile.Dataset.WriteToXml());

            var position = xdoc
                .GetElementsByDicomKeyword("InstanceNumber")
                .Select(elt => Convert.ToInt32(elt.Value))
                .FirstOrDefault();

            var series = xdoc
                .GetElementsByDicomKeyword("SeriesDescription")
                .Select(elt => elt.Value)
                .FirstOrDefault();

            var dicomImage = new DicomImage(dicomFile.Dataset);
            var pixelBuffer = dicomImage.RenderImage().AsWriteableBitmap().PixelBuffer;
            var imageBytes = new byte[pixelBuffer.Length];
            using (var source = pixelBuffer.AsStream())
            {
                using (var dest = imageBytes.AsBuffer().AsStream())
                {
                    source.CopyTo(dest);
                }
            }

            return new ImageTransferObject()
            {
                Image = imageBytes,
                Series = series,
                Position = position,
                Width = dicomImage.Width,
                Height = dicomImage.Height
            };
        }
    }

    public static class DicomExtensions
    {
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

        public static async Task<string> GetDicomDirFileName(this StorageFolder directory)
        {
            var files = await directory.GetFilesAsync();

            return files
                .Select(file => file.Name)
                .Where(name => name.ToLower().Contains("dicomdir"))
                .FirstOrDefault();
        }

        public static async Task<IEnumerable<string>> GetImagePaths(this StorageFolder directory)
        {
            var fileName = await directory.GetDicomDirFileName();
            if (fileName != null)
            {
                return await directory.GetPathsFromDicomDirFile(fileName);
            }
            return await directory.GetPathsFromDirectory();
        }

        public static async Task<IEnumerable<string>> GetPathsFromDicomDirFile(this StorageFolder directory, string fileName)
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

        public static async Task<IEnumerable<string>> GetPathsFromDirectory(this StorageFolder directory)
        {
            var files = await directory.GetFilesAsync();
            return files
                .Select(file => file.Name);
        }
    }

    public class ImageTransferObject
    {
        public byte[] Image { get; set; }
        public string Series { get; set; }
        public int Position { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
