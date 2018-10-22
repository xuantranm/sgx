using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class LogTimeKeeper
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Devide { get; set; }

        public DateTime Date { get; set; }

        public bool Stattus { get; set; } = true;

        public string Message { get; set; }

        public string Ip { get; set; }

        public int Port { get; set; }
    }
}
