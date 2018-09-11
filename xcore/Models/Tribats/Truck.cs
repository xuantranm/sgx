using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Truck: Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        public string Code { get; set; }

        [Required]
        public string Name { get; set; }

        public string Brand { get; set; }

        [Display(Name="Ngày đăng kiểm")]
        public DateTime Register { get; set; } = DateTime.Now;

        public int BaoHanh { get; set; }

        [Display(Name = "Tải trọng")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal GrossTon { get; set; }

        [Display(Name = "Tải trọng DVT")]
        public string GrossTonDVT { get; set; }

        // Checked - Approved
        public string Status { get; set; }
    }
}
