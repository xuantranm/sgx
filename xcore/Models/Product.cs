using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Product: Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Display(Name = "Mã Hàng")]
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; }

        [Display(Name = "Mã Hàng Cũ")]
        public string CodeAccountant { get; set; }

        [Display(Name = "Tên Hàng")]
        [Required]
        public string Name { get; set; }

        [Display(Name = "ĐVT")]
        public string Unit { get; set; }

        [Display(Name = "Nhóm Hàng")]
        public string Group { get; set; }

        [Display(Name = "Phân Nhóm")]
        public string GroupDevide { get; set; }

        [Display(Name = "Vị Trí Lưu Kho")]
        public string Location { get; set; }

        [Display(Name = "Ghi Chú")]
        public string Note { get; set; }

        [Display(Name = "Số lượng")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Quantity { get; set; } = 0;

        // Khi tao phieu mua hang ben kinh doanh, trang thai chua approved, approved, van chuyen
        [Display(Name = "Số lượng (tạm giữ)")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal QuantityDonHang { get; set; } = 0;
        // Tru so luong khi Dh ben kinh doanh thanh cong

        [Display(Name = "SL Tồn Kho An Toàn")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal QuantityInStoreSafe { get; set; } = 0;

        // Int32.MaxValue memory, is 0 is no limit
        [Display(Name = "SL Tồn Kho Vượt Mức")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal QuantityInStoreMax { get; set; } = 0;

        [Display(Name = "SL Tồn Kho An Toàn Q.1")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal QuantityInStoreSafeQ1 { get; set; } = 0;

        [Display(Name = "SL Tồn Kho An Toàn Q.2")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal QuantityInStoreSafeQ2 { get; set; } = 0;

        [Display(Name = "SL Tồn Kho An Toàn Q.3")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal QuantityInStoreSafeQ3 { get; set; } = 0;

        [Display(Name = "SL Tồn Kho An Toàn Q.4")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal QuantityInStoreSafeQ4 { get; set; } = 0;

        [Display(Name = "Tình trạng")]
        public string Status { get; set; } = "Mới tạo";
    }
}
