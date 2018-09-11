using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace timekeepers.Models
{
    // Only store time keeper. Business use TimeUsers collection
    public class AttLog
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string EnrollNumber { get; set; }
        public string VerifyMode { get; set; }
        public string InOutMode { get; set; }
        public string Workcode { get; set; }
        public DateTime Date { get; set; }
    }
}
