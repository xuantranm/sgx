using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Order : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [Display(Name = "Mã đơn hàng")]
        public string Code { get; set; }

        [Required]
        [Display(Name = "Người tạo đơn hàng")]
        public string By { get; set; }

        [Required]
        [Display(Name = "Tên khách hàng")]
        public string Khach { get; set; }

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Display(Name = "Quận/Huyện")]
        public string District { get; set; }

        [Display(Name = "Thành phố/Tỉnh thành")]
        public string City { get; set; }

        [Display(Name = "Số điện thoại")]
        public string Mobile { get; set; }

        [Display(Name = "Trọng lượng")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Weight { get; set; }

        // Kg / tấn
        [Display(Name = "Loại tải trọng")]
        public string WeightUnit { get; set; }

        [Display(Name = "Xe vận chuyển")]
        public string XeCode { get; set; }

        [Display(Name = "Giá trị đơn hàng")]
        [BsonRepresentation(BsonType.Decimal128)]
        [DisplayFormat(DataFormatString = "{0:n0}")]
        public decimal Amount { get; set; }

        // Chưa xác nhận / Xác nhận/ Chuyển ĐV Vận chuyển / Đang giao / Giao thành công
        public string Status { get; set; } = "CXN";

        public IList<HangHoaMua> HangHoaMuas { get; set; }
    }

    public class HangHoaMua
    {
        [Display(Name = "Mã Hàng")]
        public string Code { get; set; }

        [Display(Name = "Tên Hàng")]
        public string Name { get; set; }

        [Display(Name = "Nhóm Hàng")]
        public string Group { get; set; }

        [Display(Name = "Phân Nhóm")]
        public string GroupDevide { get; set; }

        [Display(Name = "Số lượng mua")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Quantity { get; set; } = 0;

        [Display(Name = "Đơn giá")]
        [BsonRepresentation(BsonType.Decimal128)]
        [DisplayFormat(DataFormatString = "{0:n0}")]
        public decimal Price { get; set; } = 0;

        [Display(Name = "Thuế GTGT (%)")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Vat { get; set; } = 0;

        [Display(Name = "Giảm giá (%)")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Discount { get; set; } = 0;

        [Display(Name = "Loại tiền")]
        public string Currency { get; set; } = Constants.Currency.Vietnamese;

        private decimal amount = 0;

        [Display(Name = "Thành tiền")]
        [DisplayFormat(DataFormatString = "{0:n0}")]
        public decimal PriceTotal
        {
            get
            {
                // giảm giá trước thuế
                var price1 = Quantity * Price;
                var discount = price1 * Discount / 100;
                var pricediscount = price1 - discount;
                var vat = pricediscount * Vat / 100;
                amount = (pricediscount + vat);
                return amount;
            }
            set
            {
                amount = value;
            }
        }
    }
}
