using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// 
    /// </summary>
    public class CreditEmployee
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public string EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string FullName { get; set; }

        public string EmployeePart { get; set; }

        public string EmployeeDepartment { get; set; }

        public string EmployeeTitle { get; set; }

        public int EmployeeKhoi { get; set; } = (int)EKhoiLamViec.SX;

        public int Type { get; set; } = (int)ECredit.UngLuong;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Money { get; set; } = 0;

        // Ngay bat dau muon.
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DateCredit { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime? DateFirstPay { get; set; }

        //6,9,12,24,...
        public int ThoiHanVay { get; set; } = 0;

        public double LaiSuat { get; set; } = 0;

        public decimal MucThanhToanHangThang { get; set; } = 0;

        public int SoLanDaThanhToan { get; set; } = 0;

        public int SoLanTraCham { get; set; } = 0;

        public int SoLanKhongTra { get; set; } = 0;

        // 0: no, 1: dang tra; 2: tra het
        public int Status { get; set; } = (int)ECreditStatus.New;

        public string Description { get; set; }

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;
    }
}
