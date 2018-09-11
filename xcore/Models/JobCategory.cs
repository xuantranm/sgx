using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // 10 first code use notice system.
    public class JobCategory : Extension
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [Required]
        public int Code { get; set; }

        // 0 mean no.
        public int ParentCode { get; set; } = 0;

        [Required]
        public string Name { get; set; }

        // Use link base alias
        public string Alias { get; set; }

        public string Description { get; set; }

        public int Order { get; set; } = 1;

        [Required]
        public string Language { get; set; } = Constants.Languages.Vietnamese;

        public bool Enable { get; set; } = true;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;
        public string ModifiedUserId { get; set; }
    }
}
