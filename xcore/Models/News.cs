using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Thông báo của công ty
    /// Tin tức
    /// ...
    /// each language 1 record. group by code
    /// Create/Edit 1 language 1 time
    /// </summary>
    public class News: Extension
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // 1 code for languages
        // 1-> to auto
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

        // Base Modified date
        public bool HomePage { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedUserId { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public string ModifiedUserId { get; set; }
    }
}