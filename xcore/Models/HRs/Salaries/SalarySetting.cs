using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Cai dat: 
    /// 1. Mau so: 26 (mac dinh),27 (fucture),30 (bao ve)
    /// </summary>
    public class SalarySetting
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // mau-so-lam-viec (26) | mau-so-bao-ve (30) | mau-so-khac (27)
        public string Key { get; set; }

        public string Value { get; set; }

        // use show text
        public string Title { get; set; }

        [Display(Name = "Mô tả")]
        public string Description { get; set; }
        
        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}
