using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class ProductLog: Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Display(Name ="Mã hàng")]
        public string Code { get; set; }

        [Display(Name = "Tên hàng")]
        public string Name { get; set; }

        // Nhập / Xuất
        [Display(Name = "Loại")]
        public string Type { get; set; }

        [Display(Name = "Số lượng ban đầu")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Quantity { get; set; } = 0;

        [Display(Name = "Số lượng thay đổi")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal QuantityChange { get; set; } = 0;

        [Display(Name = "Phiếu yêu cầu")]
        public string Request { get; set; }

        [Display(Name = "Phiếu đặt hàng")]
        public string DatHang { get; set; }

        [Display(Name = "Phiếu nhận hàng")]
        public string NhanHang { get; set; }

        // Kho xuất cho kinh doanh (KD)/ nha máy (NM)/ trả nhà cung cấp (CC)
        [Display(Name = "Phiếu xuất hàng")]
        public string XuatHang { get; set; }
    }
}
