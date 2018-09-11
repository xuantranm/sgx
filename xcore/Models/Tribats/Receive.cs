using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Receive : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [Display(Name ="Phiếu nhận hàng")]
        public string Code { get; set; }

        [Display(Name = "Người nhận hàng")]
        public string By { get; set; }

        [Display(Name = "Tình trạng")]
        public string Status { get; set; }

        [Display(Name = "Phiếu đặt hàng")]
        public string DatHangCode { get; set; }

        [Display(Name = "Nhà cung cấp")]
        public string Supplier { get; set; }

        public IList<ReceiveItem> ReceiveItems { get; set; }
    }

    public class ReceiveItem
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

        [Display(Name = "Tình trạng")]
        public string Status { get; set; } = Constants.Status.Complete;
    }
}
