using Common.Enums;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace Models
{
    public class EmailGroup
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; }

        public string Object { get; set; }

        public bool Status { get; set; } = false;

        public int Type { get; set; } = (int)EEmailGroup.New;

        public string CreatedOn { get; set; } = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff");

        public string UpdatedOn { get; set; } = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff");
    }
}
