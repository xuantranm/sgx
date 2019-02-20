using Common.Enums;
using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class FactoryProductDinhMuc: Common
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Month { get; set; } = DateTime.Now.Month;

        public int Year { get; set; } = DateTime.Now.Year;

        public int Type { get; set; } = (int)EDinhMuc.DongGoi;

        public string ProductId { get; set; }

        public string ProductCode { get; set; }

        public int Sort { get; set; } = 0;

        public double SoBaoNhomNgay { get; set; } = 0;

        public double DinhMucTheoNgay { get; set; } = 0;

        public double DinhMucGioQuiDinh { get; set; } = 0;

        public double DinhMucTheoGio { get; set; } = 0;

        // Tự động tính
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DonGia { get; set; } = 0;

        // = DonGia hoặc tự điều chỉnh
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DonGiaDieuChinh { get; set; } = 0;

        // FactoryProductDinhMucTangCa
        public double DonGiaTangCaPhanTram { get; set; } = 10;

        // = DonGiaDieuChinh * DonGiaTangCaPhanTram
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DonGiaTangCa { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DonGiaM3 { get; set; } = 0;

        // = DonGiaM3 * DonGiaTangCaPhanTram
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DonGiaTangCaM3 { get; set; } = 0;
    }
}
