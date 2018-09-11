using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace timekeepers.Models
{
    public class UserInfoRelation
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int MachineNumber { get; set; }
        public string EnrollNumber { get; set; }
        public string Name { get; set; }
        public int FingerIndex { get; set; }
        public string TmpData { get; set; }
        public int Privelage { get; set; }
        public string Password { get; set; }
        public bool Enabled { get; set; }
        public string iFlag { get; set; }

        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }

    }
}
