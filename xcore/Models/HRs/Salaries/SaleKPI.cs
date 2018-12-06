using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// KPI từng tháng,
    /// Gọi chịu khó groupby Type and Condition.
    /// Mục đích mở rộng về sau.
    /// Hay mỗi type 1 field...
    /// Mở rộng hay thêm bớt thêm field data.
    /// </summary>
    public class SaleKPI
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public string ChucVu { get; set; }

        public string ChucVuAlias { get; set; }

        public string ChucVuCode { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal KHMoi { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DoPhuTren80 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NganhHangDat704Nganh { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DoanhThuTren80 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DoanhThuDat100 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DoanhSoTren80 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DoanhSoDat100 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DoanhSoTren120 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Total { get; set; } = 0;

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}
