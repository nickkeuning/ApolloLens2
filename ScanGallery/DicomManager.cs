using Dicom;
using Dicom.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml.Media.Imaging;

namespace ScanGallery
{
    class DicomManager
    {
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
    }
}
