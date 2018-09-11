using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class News: Extension
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        public int Code { get; set; }

        public int CategoryCode { get; set; } = 0;

        [Required]
        public string Name { get; set; }

        public string Alias { get; set; }

        public string Description { get; set; }

        public string Content { get; set; }

        public IList<Image> Images { get; set; }

        public string Source { get; set; }

        public string SourceLink { get; set; }

        [Required]
        public string Language { get; set; } = Constants.Languages.Vietnamese;

        public bool Enable { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedUserId { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public string ModifiedUserId { get; set; }
    }
}