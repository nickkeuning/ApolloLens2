using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using Windows.Storage.Streams;
using System.IO;
using Dicom;
using Dicom.Serialization;
using Dicom.Imaging;
using Windows.UI.Xaml.Media.Imaging;
using System.Runtime.InteropServices.WindowsRuntime;

namespace ScanGallery
{
    class DicomManager
    {
        private async Task<MemoryStream> GetDicomFileStream()
        {
            var filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            filePicker.FileTypeFilter.Add("*");

            var file = await filePicker.PickSingleFileAsync();
            var fStream = await file.OpenAsync(FileAccessMode.Read);
            var reader = new DataReader(fStream.GetInputStreamAt(0));
            var bytes = new byte[fStream.Size];
            await reader.LoadAsync((uint)fStream.Size);
            reader.ReadBytes(bytes);

            return new MemoryStream(bytes);
        }

        private async Task<IReadOnlyList<IStorageFile>> GetDicomFiles()
        {
            var filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.Desktop;
            filePicker.FileTypeFilter.Add("*");

            return await filePicker.PickMultipleFilesAsync();
        }

        public async Task<IEnumerable<WriteableBitmap>> GetImages()
        {
            var files = await this.GetDicomFiles();

            var images = new List<WriteableBitmap>();

            foreach (var file in files)
            {
                var stream = await file.OpenStreamForReadAsync();
                var dicomFile = await DicomFile.OpenAsync(stream);
                var dicomImage = new DicomImage(dicomFile.Dataset);
                images.Add(dicomImage.RenderImage().AsWriteableBitmap());
            }

            return images;
        }

        public async Task<WriteableBitmap> GetImage()
        {
            var stream = await this.GetDicomFileStream();
            var res = await DicomFile.OpenAsync(stream);
            var image = new DicomImage(res.Dataset);
            var rendered = image.RenderImage();
            return rendered.AsWriteableBitmap();
        }
    }
}
