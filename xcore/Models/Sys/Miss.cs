using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class Miss
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public ObjectId Id { get; set; }
       
        // vd: update finger| update leave |...
        public string Type { get; set; }

        public string Object { get; set; }

        public string Error { get; set; }

        public string DateTime { get; set; }
    }
}
