using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Request : Common
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [Display(Name ="Phiếu yêu cầu")]
        public string Code { get; set; }

        [Display(Name = "Người yêu cầu")]
        public string By { get; set; }

        public IList<RequestItem> RequestItems { get; set; }

        // Mở | Hoàn tất
        [Display(Name = "Tình trạng")]
        public string Status { get; set; }
    }

    public class RequestItem
    {
        [Display(Name = "Mã Hàng")]
        public string Code { get; set; }

        [Display(Name = "Tên Hàng")]
        public string Name { get; set; }

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
        public string Status { get; set; } = Constants.Status.Open;
    }
}
