using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Common.Enums;

namespace Models
{
    public class Domain
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }

        public bool Enable { get; set; } = true;

        public int Code { get; set; } // store images folder

        public string Language { get; set; } = Constants.Languages.Vietnamese;
    }
}
