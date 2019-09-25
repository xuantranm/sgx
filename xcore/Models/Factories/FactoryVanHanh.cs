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

        public string CongDoanId { get; set; }

        public string CongDoanCode { get; set; }

        public string CongDoanName { get; set; }

        public string CongDoanAlias { get; set; }

        public string CongDoanNoiDung { get; set; }

        public string LOT { get; set; }

        public string XeCoGioiMayId { get; set; }

        public string XeCoGioiMayName { get; set; }

        public string XeCoGioiMayAlias { get; set; }

        public string ProductId { get; set; }

        public string ProductName { get; set; }

        public string ProductAlias { get; set; }

        public string ProductType { get; set; }

        public string Employee { get; set; } // Can be "thuê ngoài,..."

        public string CaLamViec { get; set; }

        public string CaAlias { get; set; }

        [Display(Name = "Thời gian bắt đâu")]
        public TimeSpan Start { get; set; }

        public TimeSpan End { get; set; }

        public TimeSpan ThoiGianBTTQ { get; set; }

        public TimeSpan ThoiGianXeHu { get; set; }

        public TimeSpan ThoiGianNghi { get; set; }

        [Display(Name = "Thời gian CV khác")]
        public TimeSpan ThoiGianCVKhac { get; set; }

        public TimeSpan ThoiGianLamViec { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SoLuongThucHien { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Dau { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Nhot10 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Nhot50 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Nhot90 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Nhot140 { get; set; } = 0;

        [Display(Name = "Nguyên nhân")]
        public string NguyenNhan { get; set; }

        public string PhieuInCa { get; set; }
    }
}
