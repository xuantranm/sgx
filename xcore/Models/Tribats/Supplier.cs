using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Supplier:Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [Display(Name = "Nhà cung cấp")]
        public string Name { get; set; }

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Display(Name = "Quận/Huyện")]
        public string District { get; set; }

        [Display(Name = "Thành phố/Tỉnh thành")]
        public string City { get; set; }

        [Display(Name = "Số điện thoại")]
        public string Mobile { get; set; }
    }
}
