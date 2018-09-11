using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class Report
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Type { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Value { get; set; }

        // updating rules
        public string Times { get; set; }

        public DateTime DateTime { get; set; } = DateTime.Now;
    }
}
