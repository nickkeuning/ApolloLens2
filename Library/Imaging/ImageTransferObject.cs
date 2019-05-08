using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApolloLensLibrary.Imaging
{
    /// <summary>
    /// Minimum data needed to describe an image for
    /// transfer out of DicomParser or over DicomNetworking
    /// </summary>
    public class ImageTransferObject
    {
        public byte[] Image { get; set; }
        public string Series { get; set; }
        public int Position { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
