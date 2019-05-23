using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Models
{
    /// <summary>
    /// [Lương Khối Văn Phòng] (Vị Trí Thang Bang Lương)
    /// 1. THUC TE: Law:false. Base ViTri. -> FURURE: update VITRI thuộc MaSo
    /// </summary>

    // Moi chuc vu cong viec, 1 bac luong 1 record.
    // Tạo mới bac luong: foreach chuc vu, tao theo 1 record cho bac luong do.
    // Truy suat simple hơn de chung.
    // List: group by query theo chuc vu, bac luong, set value....

    public class SalaryThangBangLuong
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Ngach Luong
        public string MaSo { get; set; }

        // Ngach Luong
        public string Name { get; set; }

        public string NameAlias { get; set; }

        public string ViTriId { get; set; }

        public string ViTriCode { get; set; }

        public string ViTriName { get; set; }

        public string ViTriAlias { get; set; }

        public string TypeRole { get; set; }

        public string TypeRoleAlias { get; set; }

        public string TypeRoleCode { get; set; }

        public int Bac { get; set; }

        public double TiLe { get; set; } = 0;

        public double HeSo { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal MucLuong { get; set; }

        public bool Law { get; set; } = false;

        public bool Enable { get; set; } = true;

        /// <summary>
        /// Thoi gian ap dung.
        /// Dung de cap nhat thang luong moi, lich sử,...
        /// Get lastest base Month + Year
        /// </summary>
        public int Month { get; set; } = DateTime.Now.Month;

        public int Year { get; set; } = DateTime.Now.Year;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;
    }
}
