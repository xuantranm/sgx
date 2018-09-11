using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class FactoryVanHanh:Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; } = 0;

        public int Month { get; set; } = 0;

        public int Week { get; set; } = 0;

        public int Day { get; set; } = 0;

        [Display(Name = "Ngày")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        [Required]
        public DateTime Date { get; set; }

        public string Ca { get; set; }

        [Display(Name = "Ca làm việc")]
        public string CaLamViec { get; set; }

        public string CaAlias { get; set; }

        [Display(Name = "Mảng công việc")]
        public string MangCongViec { get; set; }

        public string MangCongViecAlias { get; set; }

        [Display(Name = "Công đoạn")]
        public string CongDoan { get; set; }

        public string CongDoanAlias { get; set; }

        public string LOT { get; set; }

        [Display(Name = "Xe cơ giới/máy")]
        public string XeCoGioiMay { get; set; }

        public string XeCoGioiMayAlias { get; set; }

        [Display(Name = "NVL/TP")]
        public string ProductId { get; set; }

        public string NVLTP { get; set; }

        public string NVLTPAlias { get; set; }

        public string NVLTPType { get; set; }

        [Display(Name = "SL công nhân")]
        [Required]
        public int SLNhanCong { get; set; } = 0;

        [Display(Name = "Thời gian bắt đâu")]
        public TimeSpan Start { get; set; }

        [Display(Name = "Thời gian kết thúc")]
        public TimeSpan End { get; set; }

        [Display(Name = "Thời gian BTTQ")]
        public TimeSpan ThoiGianBTTQ { get; set; }

        [Display(Name = "Thời gian xe hư")]
        public TimeSpan ThoiGianXeHu { get; set; }

        [Display(Name = "Thời gian nghỉ")]
        public TimeSpan ThoiGianNghi { get; set; }

        [Display(Name = "Thời gian CV khác")]
        public TimeSpan ThoiGianCVKhac { get; set; }

        [Display(Name = "Thời gian đậy/mở bạt")]
        public TimeSpan ThoiGianDayMoBat { get; set; }

        [Display(Name = "Thời gian bốc hàng")]
        public TimeSpan ThoiGianBocHang { get; set; }

        [Display(Name = "Thời gian làm việc")]
        public TimeSpan ThoiGianLamViec { get; set; }

        [Display(Name = "Số lượng thực hiện")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SoLuongThucHien { get; set; } = 0;

        [Display(Name = "Số lượng đóng gói")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SoLuongDongGoi { get; set; } = 0;  				

        [Display(Name = "SL bốc hàng")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SoLuongBocHang { get; set; } = 0;

        [Display(Name = "Dầu")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Dau { get; set; } = 0;

        [Display(Name = "Nhớt 10")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Nhot10 { get; set; } = 0;

        [Display(Name = "Nhớt 50")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Nhot50 { get; set; } = 0;

        [Display(Name = "Nhớt 90")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Nhot90 { get; set; } = 0;

        [Display(Name = "Nhớt 140")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Nhot140 { get; set; } = 0;

        [Display(Name = "Nguyên nhân")]
        public string NguyenNhan { get; set; }

        [Display(Name = "Tổng thời gian bóc hàng (giờ:phút:giây)")]
        // Store second, display convert to hh:mm:ss
        public double TongThoiGianBocHang { get; set; } = 0;

        [Display(Name = "Tổng thời gian đóng gói (giờ:phút:giây)")]
        // Store second, display convert to hh:mm:ss
        public double TongThoiGianDongGoi { get; set; } = 0;

        [Display(Name = "Tổng thời gian CV khác (giờ:phút:giây)")]
        // Store second, display convert to hh:mm:ss
        public double TongThoiGianCVKhac { get; set; } = 0;

        [Display(Name = "Tổng thời gian đậy/mở bạt (giờ:phút:giây)")]
        // Store second, display convert to hh:mm:ss
        public double TongThoiGianDayMoBat { get; set; } = 0;

        // format: MM-[Code] : tính theo xe/ngày
        public string PhieuInCa { get; set; }
    }
}
