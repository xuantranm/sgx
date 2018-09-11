using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace timekeepers.Models
{
    public class TimeKeeper
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Ip { get; set; }
        public string Port { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }
}
