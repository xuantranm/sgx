using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Unit : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Factory | Logistics | ...
        [Display(Name = "Loại")]
        public string Type { get; set; }

        [Required]
        [Display(Name="Tên")]
        public string Name { get; set; }

        public string Alias { get; set; }
    }
}
