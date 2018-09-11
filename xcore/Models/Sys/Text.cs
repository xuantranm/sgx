using Attributes;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // 10 first code use notice system.
    public class Text
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [DynamicDisplayName(1)]
        public int Code { get; set; }

        [Required]
        public string Content { get; set; }

        public string ContentPlainText { get; set; }

        public string ToolTip { get; set; }

        public string Seo { get; set; }

        public string Type { get; set; }

        [Required]
        public string Language { get; set; } = Constants.Languages.Vietnamese;

        public bool Enable { get; set; } = true;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public string ModifiedUserId { get; set; }
    }
}
