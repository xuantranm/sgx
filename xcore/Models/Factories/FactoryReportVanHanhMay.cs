using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class FactoryReportVanHanhMay
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

        public string May { get; set; }

        public string MayAlias { get; set; }

        public TimeSpan Start { get; set; }

        public TimeSpan End { get; set; }

        public TimeSpan ThoiGianLamViec { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SoLuongThucHien { get; set; } = 0;
    }
}
