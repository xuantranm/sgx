using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class OrderDetail : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [Display(Name ="Phiếu đặt hàng")]
        public string Code { get; set; }

        [Display(Name = "Người đặt hàng")]
        public string By { get; set; }

        [Display(Name = "Nhà cung cấp")]
        public string Supplier { get; set; }

        public IList<OrderItem> ProductDatHangs { get; set; }
        
        // Mở | Hoàn tất
        [Display(Name = "Tình trạng")]
        public string Status { get; set; }
    }

    public class OrderItem
    {
        [Display(Name = "Mã Hàng")]
        public string Code { get; set; }

        [Display(Name = "Tên Hàng")]
        public string Name { get; set; }

        [Display(Name = "Phiếu yêu cầu")]
        public string RequestCode { get; set; }

        [Display(Name = "Số lượng yêu cầu")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Quantity { get; set; } = 0;

        [Display(Name = "Số lượng đặt hàng")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal QuantityOrder { get; set; } = 0;

        [Display(Name = "Số lượng nhận hàng")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal QuantityReceive { get; set; } = 0;

        [Display(Name = "Đơn giá")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Price { get; set; } = 0;

        [Display(Name = "Thuế GTGT (%)")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Vat { get; set; } = 0;

        [Display(Name = "Giảm giá (%)")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Discount { get; set; } = 0;

        [Display(Name = "Loại tiền")]
        public string Currency { get; set; }

        [Display(Name = "Tình trạng")]
        public string Status { get; set; }

        // Use for check hang in store.
        //[Display(Name = "Số lượng nhận hàng")]
        //[BsonRepresentation(BsonType.Decimal128)]
        //public decimal QuantityInStore { get; set; } = 0;
    }
}
