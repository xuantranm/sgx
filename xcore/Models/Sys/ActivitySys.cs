using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class ActivitySys
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        // Get config file
        public string Environment { get; set; }

        [Required]
        public int Type { get; set; } = (int)ELogType.Other;

        [Required]
        // Performance format db:x;ui:x (x is second run times)
        // Other format update later
        public string Content { get; set; }
    }
}
