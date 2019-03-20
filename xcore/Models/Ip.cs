using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Net;

namespace Models
{
    public class Ip
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public IPAddress RemoteIpAddress { get; set; }

        public string IpAddress { get; set; }

        public string Login { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
