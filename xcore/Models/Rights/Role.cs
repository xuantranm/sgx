using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class Role: Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Display(Name = "Quyền")]
        [Required]
        public string Object { get; set; }

        // ex: nhan-su, hanh-chinh, nghi-phep, luong
        public string Alias { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }

        // = days. 0 is none
        public int Duration { get; set; } = 0;


    }
}
