using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// KPI từng tháng
    /// </summary>
    public class SaleKPI
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public string ChucVu { get; set; }

        public string ChucVuAlias { get; set; }

        public string ChucVuCode { get; set; }

        // khách hàng mới | độ phủ | doanh số,...
        public string Type { get; set; }

        public string TypeAlias { get; set; }

        public string TypeCode { get; set; }

        // null-empty not set; tren 80, 90,....
        public string Condition { get; set; }

        public string ConditionValue { get; set; }

        public string Value { get; set; }

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}
