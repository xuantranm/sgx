using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace Models
{
    public class ContentIn
    {
        public string Detail { get; set; }

        public IList<Img> Imgs { get; set; }

        public IList<Document> Documents { get; set; }

        public IList<string> Videos { get; set; }

        public int ShowMethod { get; set; }

        public bool IsDelete { get; set; } = false; // Use edit, delete rule
    }
}
