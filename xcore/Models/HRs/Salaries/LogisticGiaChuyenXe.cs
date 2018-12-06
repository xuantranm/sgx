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
    public class LogisticGiaChuyenXe
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public string Tuyen { get; set; }

        public string TuyenAlias { get; set; }

        // base location: HOCHIMINH | BIENHOA | DONGNAI
        public string TuyenCode { get; set; }

        public string LoaiXe { get; set; }

        public string LoaiXeAlias { get; set; }

        // XN | XL
        public string LoaiXeCode { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongNangSuatChuyenCom { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal HoTroTienComTinh { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongNangSuatChuyen { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen1 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen2 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen3 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen4 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen5 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal HoTroChuyenDem { get; set; } = 0;

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}
