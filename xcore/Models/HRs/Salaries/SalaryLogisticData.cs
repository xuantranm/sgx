using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Cai dat: 
    /// 1. Mau so: 26 (mac dinh),27 (fucture),30 (bao ve)
    /// </summary>
    public class SalaryLogisticData
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public string EmployeeId { get; set; }

        public string MaNhanVien { get; set; }

        public string FullName { get; set; }

        public string ChucVu { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DoanhThu { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongTheoDoanhThuDoanhSo { get; set; }

        public IList<LogisticChuyenGia> LogisticChuyenGias { get; set; }

        public int TongSoChuyen { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TienChuyen { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CongTacXa { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal KhoiLuongBun { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DonGiaBun { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThanhTienBun { get; set; }

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }

    public class LogisticChuyenGia
    {
        public string ViTri { get; set; }
        public string LoaiXe { get; set; }
        public string TenChuyen { get; set; }
        public string ChuyenName { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SoLuongTinhDonGia { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DonGia { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SoLuongTinhCongTacXa { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CongTacXa { get; set; }
    }
}
