using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace Models
{
    public class FactoryReportXCG
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

        public string XeCoGioi { get; set; }

        public string XeCoGioiAlias { get; set; }

        public string CongDoan { get; set; }

        public TimeSpan Start { get; set; }

        public TimeSpan End { get; set; }

        public TimeSpan ThoiGianBTTQ { get; set; }

        public TimeSpan ThoiGianXeHu { get; set; }

        public TimeSpan ThoiGianLamViec { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal SoLuongThucHien { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Dau { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Nhot10 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Nhot50 { get; set; } = 0;
    }
}
