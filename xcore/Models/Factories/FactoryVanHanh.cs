using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class FactoryVanHanh : Extension
    {
        public int Year { get; set; } = 0;

        public int Month { get; set; } = 0;

        public int Week { get; set; } = 0;

        public int Day { get; set; } = 0;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [Required]
        public DateTime Date { get; set; }

        public string Ca { get; set; }

        public string CaId { get; set; }

        public string CaAlias { get; set; }

        public string CongDoanId { get; set; }

        public string CongDoanCode { get; set; }

        public string CongDoanName { get; set; }

        public string CongDoanAlias { get; set; }

        public string CongDoanNoiDung { get; set; }

        public string LOT { get; set; }

        public string XeCoGioiMayId { get; set; }

        public string XeCoGioiMayCode { get; set; }

        public string XeCoGioiMayName { get; set; }

        public string XeCoGioiMayAlias { get; set; }

        public string ProductId { get; set; }

        public string ProductName { get; set; }

        public string ProductAlias { get; set; }

        public string ProductType { get; set; }

        public string EmployeeId { get; set; }

        public string Employee { get; set; } // Can be "thuê ngoài,..."

        public string EmployeeAlias { get; set; }

        public string CaLamViec { get; set; }

        public string CaLamViecId { get; set; }

        public string CaLamViecAlias { get; set; }

        public TimeSpan Start { get; set; }

        public TimeSpan End { get; set; }

        public TimeSpan ThoiGianBTTQ { get; set; }

        public TimeSpan ThoiGianXeHu { get; set; }

        public TimeSpan ThoiGianNghi { get; set; }

        public TimeSpan ThoiGianCVKhac { get; set; }

        public TimeSpan ThoiGianLamViec { get; set; }

        public double SoLuongThucHien { get; set; } = 0;

        public double Dau { get; set; } = 0;

        public double Nhot10 { get; set; } = 0;

        public double Nhot50 { get; set; } = 0;

        public double Nhot90 { get; set; } = 0;

        public double Nhot140 { get; set; } = 0;

        public string NguyenNhan { get; set; }

        public string PhieuInCa { get; set; }

        public int Status { get; set; } = (int)EVanHanhStatus.DangXuLy;
    }
}
