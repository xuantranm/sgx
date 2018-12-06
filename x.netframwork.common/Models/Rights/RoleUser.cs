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

        public string User { get; set; }

        public string FullName { get; set; }

        public string Role { get; set; }

        // 0: none, 1: read, 2: add, 3: edit, 4: disable, 5: delete (mean reactive)
        public int Action { get; set; } = 0;

        public DateTime Start { get; set; } = DateTime.Now;

        public DateTime? Expired { get; set; }
    }
}
