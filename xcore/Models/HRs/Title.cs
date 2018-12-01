using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // SalaryThangBangLuongs
    public class Title
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Code { get; set; }

        [Required]
        public string Name { get; set; }

        public string Alias { get; set; }

        public string Description { get; set; }

        public int Order { get; set; } = 1;

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;
    }
}
