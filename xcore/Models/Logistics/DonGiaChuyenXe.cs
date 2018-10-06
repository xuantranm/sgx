using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// KPI từng tháng
    /// </summary>
    public class DonGiaChuyenXe
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public string ChucVu { get; set; }

        public string ChucVuAlias { get; set; }

        public string ChucVuCode { get; set; }

        // TP.HCM Xe nhỏ 1.7 tấn 				
        // TP.HCM Xe lớn ben và 8 tấn 				
        public string ViTriLoaiXe { get; set; }

        // 1-> 5
        public int Chuyen { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Money { get; set; }

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}
