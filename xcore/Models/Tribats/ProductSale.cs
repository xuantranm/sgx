using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // 10 first code use notice system.
    public class ProductSale : Extension
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        public int Code { get; set; }

        public int CategoryCode { get; set; }

        [Required]
        public string Name { get; set; }

        public string Alias { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal? Price { get; set; } = 0;

        public string Description { get; set; }

        public string Content { get; set; }

        public IList<Image> Images { get; set; }

        [Required]
        public string Language { get; set; } = Constants.Languages.Vietnamese;

        public string Status { get; set; } = "Active";

        public bool Enable { get; set; } = true;

        // Base Modified date
        public bool HomePage { get; set; } = false;

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public string CreatedUserId { get; set; }
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public string ModifiedUserId { get; set; }
    }
}
