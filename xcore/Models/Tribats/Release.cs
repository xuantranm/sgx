using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Release : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // Kinh Doanh/ Nhà Máy
        [Display(Name = "Loại xuất hàng")]
        [Required]
        public string Type { get; set; }

        [Display(Name = "Phiếu xuất hàng")]
        [Required]
        public string Code { get; set; }

        [Display(Name = "Người xuất hàng")]
        public string By { get; set; }

        // Kinh Doanh: OrderCode ; Nhà Máy: Code NM
        [Display(Name = "Phiếu yêu cầu")]
        public string PhieuYeuCau { get; set; }

        public IList<ReleaseItem> ReleaseItems { get; set; }
    }

    public class ReleaseItem
    {
        [Display(Name = "Mã Hàng")]
        public string Code { get; set; }

        [Display(Name = "Tên Hàng")]
        public string Name { get; set; }

        [Display(Name = "Số lượng xuất")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Quantity { get; set; } = 0;
    }
}
