using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// ??? Delete or no. Use or no. later...
    /// <summary>
    /// Quản lý lương nhân viên: nhân viên ở Vị Trí lương, Bac + Thoi gian
    /// Nếu cập nhật, tạo thêm records.
    /// Lấy records mới nhất hoặc theo thời gian bắt đầu.
    /// </summary>
    public class SalaryThangBacLuongEmployee
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public string EmployeeId { get; set; }

        public string ViTriCode { get; set; }

        public int Bac { get; set; }

        // Muc luong hien tai. query nhanh
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal MucLuong { get; set; }

        // chua su dung. su dung sau. dung FlagReal
        // muc luong theo luat VN, false theo cong ty
        public bool Law { get; set; } = true;

        // Thuc te - true;
        // law update later
        public bool FlagReal { get; set; } = true;

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Start { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}
