using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Department: Extension
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Code { get; set; }

        public string ParentCode { get; set; }

        public string PartCode { get; set; }

        public string PartName { get; set; }

        [Required]
        public string Name { get; set; }

        public string Alias { get; set; }
        
        public string Description { get; set; }

        public IList<Image> Images { get; set; }

        public int Order { get; set; } = 1;

        public string Language { get; set; } = Constants.Languages.Vietnamese;

        public bool Enable { get; set; } = true;
    }
}
