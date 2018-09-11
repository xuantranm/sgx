using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class FactoryDanhGiaXCG: Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; } = 0;

        public int Month { get; set; } = 0;

        public int Week { get; set; } = 0;

        public int Day { get; set; } = 0;

        public string ChungLoaiXe { get; set; }

        public string ChungLoaiXeAlias { get; set; }

        public string CongViec { get; set; }

        public string CongViecALias { get; set; }

        public TimeSpan ThoiGianLamViec { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Oil { get; set; } = 0;

        public DanhGiaXCGCongDoanVanHanh CongDoanVanHanh { get; set; }

        public DanhGiaXCGNangSuat NangSuat { get; set; }

        public DanhGiaXCGTieuHaoDau TieuHaoDau { get; set; }

        public DanhGiaXCGVanHanh VanHanh { get; set; }

        public DanhGiaXCGChiPhi ChiPhi { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DanhGiaTongThe { get; set; } = 0;

        // A,B,C,D,...
        public string XepHangXCG { get; set; }

    }

    public class DanhGiaXCGCongDoanVanHanh
    {
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TrongSoDanhGia { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DiemCongDoan { get; set; } = 0;

        // Display %
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TiTrongCongDoan { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DiemDanhGiaCongDoan { get; set; } = 0;
    }

    public class DanhGiaXCGNangSuat
    {
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TrongSoDanhGia { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TieuChuanCongDoan { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucTe { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DiemDanhGiaCongDoan { get; set; } = 0;
    }

    public class DanhGiaXCGTieuHaoDau
    {
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TrongSoDanhGia { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TieuChuanCongDoan { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucTe { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DiemDanhGiaCongDoan { get; set; } = 0;
    }

    public class DanhGiaXCGVanHanh
    {
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DiemVanHanhXCG { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TrongSo { get; set; } = 0;
    }

    public class DanhGiaXCGChiPhi
    {
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TieuChuan { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucTe { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DiemChiPhiXCG { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TrongSo { get; set; } = 0;
    }
}
