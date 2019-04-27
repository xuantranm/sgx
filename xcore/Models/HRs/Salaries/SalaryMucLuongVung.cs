using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class SalaryMucLuongVung
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ToiThieuVungQuiDinh { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TiLeMucDoanhNghiepApDung { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ToiThieuVungDoanhNghiepApDung { get; set; }

        public string Unit { get; set; } = "đồng";

        public int Month { get; set; } = DateTime.Now.Month;

        public int Year { get; set; } = DateTime.Now.Year;

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;
    }
}
