using Common.Enums;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class FactoryProductDinhMucTiLe : Common
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Month { get; set; } = DateTime.Now.Month;

        public int Year { get; set; } = DateTime.Now.Year;

        // DongGoi, BocVac,...
        public int Type { get; set; } = (int)EDinhMuc.DongGoi;

        public double TiLe { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal MucLuong { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DonGia { get; set; } = 0;

        public double NgayCong { get; set; } = 0;

        public double ThoiGian { get; set; } = 0;
    }
}
