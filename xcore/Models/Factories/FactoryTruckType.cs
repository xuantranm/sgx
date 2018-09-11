using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class FactoryTruckType : Common
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        // xe cuốc: xe cuôc 03, 05, 07
        public string TypeAlias { get; set; }

        // Ben / Cuốc / Ủi / Xúc
        public string Code { get; set; }

        // ben / cuoc / ui / xuc
        public string CodeAlias { get; set; }

        [Required]
        public string Name { get; set; }

        public string Alias { get; set; }
    }
}
