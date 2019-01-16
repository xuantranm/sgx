using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class BHYTHospital: Extension
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Display(Name = "Mã KCB")]
        public string Code { get; set; }

        [Display(Name="Tuyến")]
        public string Local { get; set; }

        [Display(Name = "Tỉnh/Thành phố")]
        public string City { get; set; }

        [Display(Name = "Quận/Huyện")]
        public string District { get; set; }

        [Display(Name= "Cơ sở khám chữa bệnh")]
        public string Name { get; set; }

        public string Alias { get; set; }

        [Display(Name = "Địa chỉ")]
        public string Address { get; set; }

        [Display(Name = "Điều kiện")]
        public string Condition { get; set; }

        [Display(Name = "Ghi chú")]
        public string Note { get; set; }

        public bool Enable { get; set; } = true;

        public bool NoDelete { get; set; } = false;

        // For check use?
        public int Usage { get; set; } = 0;

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public string CreatedBy { get; set; }

        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        public string ModifiedBy { get; set; }

        // For multi update, general by system
        public string Timestamp { get; set; } = DateTime.Now.ToString("yyyyMMddHHmmssfff");
    }
}
