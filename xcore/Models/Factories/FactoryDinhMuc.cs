using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class FactoryDinhMuc: Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string CongDoan { get; set; }

        public string Alias { get; set; }

        public int Year { get; set; } = 0;

        public int Month { get; set; } = 0;

        public int Week { get; set; } = 0;

        // Future rule can be apply
        public int Day { get; set; } = 0;

        public DinhMucDiemCongDoanVanHanh DiemCongDoanVanHanh { get; set; }

        public DinhMucDiemNangSuan1h DiemNangSuan1h { get; set; }

        public DinhMucChiPhi DinhMucChiPhi { get; set; }
    }
    
    public class DinhMucDiemCongDoanVanHanh
    {
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XeCuoc { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XeBen { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XeUi { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XeXuc { get; set; } = 0;
    }

    public class DinhMucDiemNangSuan1h
    {
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XeCuoc { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XeBen { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XeUi { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XeXuc { get; set; } = 0;
    }

    public class DinhMucChiPhi
    {
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XeCuoc07 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XeCuoc05 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XeCuoc03 { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XeBen { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XeUi { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XeXuc { get; set; } = 0;
    }
}
