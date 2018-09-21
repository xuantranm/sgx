using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Only 1 record [Enable]: true.
    /// Add new record: [Enable] old up to false
    /// </summary>
    public class SalaryMucLuongVung
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Display(Name = "Mức lương tối thiểu vùng")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ToiThieuVungQuiDinh { get; set; }

        [Display(Name = "Tỉ lệ % doanh nghiệp áp dụng")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TiLeMucDoanhNghiepApDung { get; set; }

        [Display(Name = "Mức lương tối thiểu doanh nghiệp")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ToiThieuVungDoanhNghiepApDung { get; set; }

        [Display(Name = "ĐVT")]
        public string Unit { get; set; } = "đồng";

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Start { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}
