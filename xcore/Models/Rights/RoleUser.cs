using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class RoleUser : Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Display(Name = "Người dùng")]
        public string User { get; set; }

        // join later, first
        [Display(Name = "Người dùng")]
        public string FullName { get; set; }

        [Display(Name = "Quyền")]
        public string Role { get; set; }

        [Display(Name = "Phạm vi")]
        // 0: none, 1: read, 2: add, 3: edit, 4: disable, 5: delete (mean reactive)
        public int Action { get; set; } = 0;

        [Display(Name = "Ngày hiệu lực")]
        public DateTime Start { get; set; } = DateTime.Now;

        [Display(Name = "Ngày hết hiệu lực")]
        public DateTime? Expired { get; set; }
    }
}
