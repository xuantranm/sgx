using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class ContractType
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Display(Name = "Loại hợp đồng")]
        [Required]
        public string Name { get; set; }

        [Display(Name = "Số tháng")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Month { get; set; } = 0;

        public bool Enable { get; set; } = true;

        public string Language { get; set; } = Constants.Languages.Vietnamese;
    }
}
