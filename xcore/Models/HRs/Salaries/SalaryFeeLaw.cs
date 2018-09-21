using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // Luật: các loại phí bắt buộc người lao động,...
    public class SalaryFeeLaw
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // BHXH (include: bhxh-bhyt-thatnghiep),...
        [Display(Name = "Tên")]
        public string Name { get; set; }

        [Display(Name = "Tỉ lệ đóng")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TiLeDong { get; set; }

        public string Description { get; set; }

        public string NameAlias { get; set; }

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Start { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}
