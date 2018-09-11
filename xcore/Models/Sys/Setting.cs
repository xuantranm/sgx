using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class Setting
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Type { get; set; }

        [Required]
        public string Key { get; set; }

        [Required]
        public string Value { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        [Required]
        public string Language { get; set; } = Constants.Languages.Vietnamese;

        public bool Enable { get; set; } = true;

        public bool NoDelete { get; set; } = false;

        // For check use?
        public int Usage { get; set; } = 0;

        // For multi update, general by system
        public string Timestamp { get; set; } = DateTime.Now.ToString("yyyyMMddHHmmssfff");

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string CreatedBy { get; set; }

        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        public string ModifiedBy { get; set; }
    }
}
