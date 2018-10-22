using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;

namespace Models
{
    public class WorkTimeType
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public TimeSpan Start { get; set; }

        public TimeSpan End { get; set; }

        public bool Enable { get; set; } = true;

        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime UpdatedOn { get; set; } = DateTime.Now;
        public DateTime CheckedOn { get; set; } = DateTime.Now;
        public DateTime ApprovedOn { get; set; } = DateTime.Now;
        public string CreatedBy { get; set; } = string.Empty;
        public string UpdatedBy { get; set; } = string.Empty;
        public string CheckedBy { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
    }
}
