using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class FactoryReportBocHang
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; } = 0;

        public int Month { get; set; } = 0;

        public int Week { get; set; } = 0;

        public int Day { get; set; } = 0;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }

        //Tên NVL/BTP/TP
        public string Product { get; set; }

        public string ProductAlias { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SoLuongBocHang { get; set; } = 0;
    }
}
