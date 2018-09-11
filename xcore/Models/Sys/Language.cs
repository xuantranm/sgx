using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Language
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        // Apply standard ISO 639‑1 (2 character)
        // https://www.w3schools.com/tags/ref_language_codes.asp
        public string Code { get; set; }

        // en-Us, vi-VN
        public string Name { get; set; }

        // Name base country language (English, Tiếng Việt)
        public string Title { get; set; }

        public bool Enable { get; set; } = true;
    }
}
