using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class ProductV100: Common
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string Code { get; set; }

        public string CodeAccountant { get; set; }

        [Required]
        public string Name { get; set; }

        public string Unit { get; set; }

        public string Group { get; set; }

        public string GroupDevide { get; set; }

        public string Location { get; set; }

        public string Note { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Quantity { get; set; } = 0;

        [Display(Name = "Số lượng (tạm giữ)")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal QuantityDonHang { get; set; } = 0;

        [Display(Name = "SL Tồn Kho An Toàn")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal QuantityInStoreSafe { get; set; } = 0;

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
