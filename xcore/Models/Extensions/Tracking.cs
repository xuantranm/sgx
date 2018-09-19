using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class Tracking
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; }

        public string Function { get; set; }

        public string Action { get; set; }

        public string Value { get; set; }

        public string Content { get; set; }

        public DateTime Created { get; set; } = DateTime.Now;
    }
}
