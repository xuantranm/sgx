using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class BHYTHospital: Extension
    {
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

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        public DateTime ModifiedDate { get; set; } = DateTime.Now;
    }
}
