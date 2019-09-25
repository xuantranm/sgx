using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Models
{
    /// <summary>
    /// Sort base Code
    /// </summary>
    public class Document
    {
        public int Code { get; set; } = 1;

        public string FileName { get; set; }

        public string Orginal { get; set; }

        public string Path { get; set; }

        public string Title { get; set; }

        public string Extension { get; set; }

        public bool Enable { get; set; } = true;
    }
}
