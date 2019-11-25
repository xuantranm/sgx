using Common.Enums;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Models
{
    public class Property
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Type { get; set; } = (int)EData.Setting;

        public string Key { get; set; }

        public string Value { get; set; }

        public int ValueType { get; set; } = (int)EValueType.String;

        public bool IsChoose { get; set; } = true;

        public bool Enable { get; set; } = true;
    }
}
