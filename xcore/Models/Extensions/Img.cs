using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Models
{
    /// <summary>
    /// Sort base Code
    /// Image to Img : cause Image in system
    /// </summary>
    public class Img
    {
        public string Path { get; set; } // format: alias-codeEntity

        public string FileName { get; set; } // Format base system: alias-codeEntity-sizeType-orderImg

        public string Title { get; set; }

        public bool Main { get; set; }

        public int Type { get; set; } = (int)EImageSize.x1200x800;

        public bool IsDelete { get; set; } = false; // Use edit, delete rule

        public int Code { get; set; } = 1; // sort

        public string Temp { get; set; }

        public string Orginal { get; set; } // FileName orginal

        public int? Size { get; set; } // Size image

        public string TypeFile { get; set; } // TypeFile image
    }
}
