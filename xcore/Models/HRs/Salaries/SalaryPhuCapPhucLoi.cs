using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // Moi chuc vu cong viec, 1 bac luong 1 record.
    // Tạo mới bac luong: foreach chuc vu, tao theo 1 record cho bac luong do.
    // Truy suat simple hơn de chung.
    // List: group by query theo chuc vu, bac luong, set value.... 
    public class SalaryPhuCapPhucLoi
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Display(Name = "Loại")]
        //1: phu cap; 2: phuc loi
        public int Type { get; set; }

        [Display(Name = "STT")]
        public int Order { get; set; }

        [Display(Name = "Tên")]
        public string Name { get; set; }

        [Display(Name = "Mã số")]
        public string Code { get; set; }

        public string NameAlias { get; set; }

        // Theo luat VN, false theo cong ty
        public bool Law { get; set; } = true;

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Start { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}
