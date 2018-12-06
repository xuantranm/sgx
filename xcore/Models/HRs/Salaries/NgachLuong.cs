using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Áp dụng thuế, lương nhà máy, sản xuất.
    /// THUE: Law is true.
    /// Nha May, SX: Law is false.
    /// Lương VP tính theo thang bảng lương. [SalaryThangBangLuong]
    /// Mỗi bậc 1 record
    /// </summary>

    public class NgachLuong
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string ChucDanhCongViec { get; set; }

        public string Alias { get; set; }

        public string MaSo { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TiLe { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal HeSo { get; set; } = 0;

        public int Bac { get; set; } = 1;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal MucLuongThang { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal MucLuongNgay { get; set; } = 0;

        public int Order { get; set; } = 1;

        public string TypeRole { get; set; }

        public string TypeRoleAlias { get; set; }

        public string TypeRoleCode { get; set; }

        /// <summary>
        /// Thoi gian ap dung.
        /// Dung de cap nhat thang luong moi, lich sử,...
        /// Get lastest base Month + Year
        /// </summary>
        public int Month { get; set; } = DateTime.Now.Month;

        public int Year { get; set; } = DateTime.Now.Year;

        public bool Law { get; set; } = false;

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;
    }
}
