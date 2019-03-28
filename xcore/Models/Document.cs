using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Models
{
    public class Document
    {
        public string Path { get; set; }

        public string FileName { get; set; }

        public string OrginalName { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public int Order { get; set; } = 1;

        public bool Main { get; set; } = true;

        // if type of image. May be use size: + _desktop, _tablet, _mobile, _thumb, ...
        public int Type { get; set; } = (int)EFile.Document;
    }
}
