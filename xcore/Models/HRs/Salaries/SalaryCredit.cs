using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// 
    /// </summary>
    public class SalaryCredit
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

        public string PhongBan { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal UngLuong { get; set; } = 0;

        // Future build later. (more information (vay,...)

        public decimal KhoanVay { get; set; } = 0;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? NgayVay { get; set; }

        //6,9,12,24,...
        public int ThoiHanVay { get; set; } = 0;

        public decimal MucThanhToanHangThang { get; set; } = 0;

        public int SoLanTra { get; set; } = 0;

        public int SoLanTraCham { get; set; } = 0;

        public int SoLanKhongTra { get; set; } = 0;

        // 0: no, 1: dang tra; 2: tra het
        public int Status { get; set; } = 0;

        [Display(Name = "Mô tả")]
        public string Description { get; set; }
        
        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}
