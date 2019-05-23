using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class LeaveType : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        [Display(Name = "Loại phép")]
        public string Name { get; set; }

        public string Alias { get; set; }

        public string Description { get; set; }

        // Tối đa trong năm/ Số ngày phép
        [Display(Name = "Số ngày phép trong năm")]
        public double YearMax { get; set; } = 0;

        // Tối đa trong tháng
        [Display(Name = "Số ngày phép trong tháng")]
        public double? MonthMax { get; set; } = 0;

        [Display(Name = "Số ngày phép tối đa 1 lần")]
        public double? MaxOnce { get; set; } = 0;

        [Display(Name = "Trả lương")]
        public bool SalaryPay { get; set; } = true;

        [Display(Name = "Mặc định")]
        public bool Display { get; set; } = true;
    }
}
