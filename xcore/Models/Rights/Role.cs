using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Role: Common
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        public string Object { get; set; }

        public string Alias { get; set; }

        public string Description { get; set; }

        public int Duration { get; set; } = 0; // 0 is none
    }
}
