using Common.Enums;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// FactoryProductCongTheoNgay
    /// + Quản lý công theo từng ngày
    /// </summary>
    public class FactoryProductCongTheoNgay
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; } = DateTime.Now;

        public int Day { get; set; } = DateTime.Now.Day;

        public int Month { get; set; } = DateTime.Now.Month;

        public int Year { get; set; } = DateTime.Now.Year;

        public int Type { get; set; } = (int)EDinhMuc.DongGoi;

        public int Mode { get; set; } = (int)EMode.TrongGio;

        public string EmployeeId { get; set; }

        public string EmployeeCode { get; set; }

        public string EmployeeName { get; set; }

        public string EmployeeAlias { get; set; }

        public string ObjectId { get; set; }

        public string ObjectCode { get; set; }

        public string ObjectName { get; set; }

        public string ObjectAlias { get; set; }

        public int ObjectSort { get; set; } = 1;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ObjectPrice { get; set; } = 0;

        public double Value { get; set; } = 0;

        // Làm tròn ở đây. Chung 1 cách.
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Amount { get; set; } = 0;
    }
}
