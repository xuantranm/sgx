using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Combine : Nha May, VP, SX
    /// Data từng tháng.
    /// </summary>
    public class EmployeeCong
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; } = DateTime.Now.Year;

        public int Month { get; set; } = DateTime.Now.Month;

        public string EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeName { get; set; }

        public string EmployeeChucVu { get; set; }

        public int Type { get; set; } = (int)EKhoiLamViec.SX;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CongTong { get; set; } = 0;

        // Suat
        public double Com { get; set; } = 0;

        public double ComSX { get; set; } = 0;

        public double ComNM { get; set; } = 0;

        public double ComKD { get; set; } = 0;

        public double ComVP { get; set; } = 0;

        public double TongThoiGianCongViecKhacTrongGio { get; set; } = 0;

        public double TongThoiGianCongViecKhacNgoaiGio { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TongCongCongViecKhacTrongGio { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TongCongCongViecKhacNgoaiGio { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThanhTienTrongGio { get; set; } = 0;

        public decimal ThanhTienNgoaiGio { get; set; } = 0;

        public bool Enable { get; set; } = true;

        public string CreatedOn { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        public string UpdatedOn { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

    }
}
