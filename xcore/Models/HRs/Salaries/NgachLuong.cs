using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Áp dụng thuế, lương nhà máy, sản xuất.
    /// THUE: Law is true.
    /// Nha May, SX: Law is false.
    /// Lương VP tính theo thang bảng lương. [SalaryThangBangLuong]
    /// Mỗi bậc 1 record
    /// 
    /// /// <summary>
    /// [Lương Khối Văn Phòng] (Vị Trí Thang Bang Lương)
    /// 1. THUC TE: Law:false. Base ViTri. -> FURURE: update VITRI thuộc MaSo
    /// </summary>

    // Moi chuc vu cong viec, 1 bac luong 1 record.
    // Tạo mới bac luong: foreach chuc vu, tao theo 1 record cho bac luong do.
    // Truy suat simple hơn de chung.
    // List: group by query theo chuc vu, bac luong, set value....
    /// </summary>

    public class NgachLuong
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Type { get; set; } = (int)ESalary.VP;

        public string Code { get; set; }

        public string Name { get; set; }

        public string Alias { get; set; }

        public double Rate { get; set; } = 0;

        public int Level { get; set; } = 1;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Money { get; set; } = 0;

        public string TypeRole { get; set; }

        public int Order { get; set; } = 1;

        public int Month { get; set; } = DateTime.Now.Month;

        public int Year { get; set; } = DateTime.Now.Year;

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        public string CreatedBy { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;
        public string UpdatedBy { get; set; }
    }
}
