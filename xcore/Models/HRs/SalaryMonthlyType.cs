using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class SalaryMonthlyType: Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Order { get; set; } = 1;

        // If type null, is type.
        // Ex: Thu nhập, Thu nhập khác, Các khoản giảm trừ
        public string Type { get; set; }

        [Display(Name ="Nội dung lương")]
        public string Title { get; set; }

        public string Alias { get; set; }

        // day, hour, ...
        [Display(Name = "ĐVT")]
        public string Unit { get; set; }

        // No use now, future...
        public string Description { get; set; } = string.Empty;
    }
}
