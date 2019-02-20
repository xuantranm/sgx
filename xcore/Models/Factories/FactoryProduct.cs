using Common.Enums;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    // NVL/BTP/TP
    public class FactoryProduct : Common
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Type { get; set; } = (int)EProductType.TP;

        public string Code { get; set; } 

        public string Name { get; set; }

        public string Alias { get; set; }

        // 2 , 4, 6,...
        public string Group { get; set; }

        public string Unit { get; set; }

        // Big number
        public decimal Quantity { get; set; } = 0;

        public int Sort { get; set; } = 1;
    }
}
