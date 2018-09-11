using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // NVL/BTP/TP
    public class FactoryProduct : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // NVL/BTP/TP
        [Display(Name = "Loại")]
        public string Type { get; set; }

        [Required]
        [Display(Name="Tên")]
        public string Name { get; set; }

        public string Alias { get; set; }

        [Display(Name="ĐVT")]
        public string Unit { get; set; }

        [Display(Name = "Số lượng")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Quantity { get; set; } = 0;
    }
}
