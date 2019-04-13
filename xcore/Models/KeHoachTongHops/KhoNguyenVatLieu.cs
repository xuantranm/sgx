using Common.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// [Query fast, Update lower]
    /// Product: Name, Code, Unit for faster.
    /// If change, update all.
    /// </summary>
    public class KhoNguyenVatLieu: CommonV101
    {
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }

        public string ProductId { get; set; }

        public string ProductName { get; set; }

        public string ProductAlias { get; set; }

        public string ProductCode { get; set; }

        public string ProductUnit { get; set; }

        public double TonDau { get; set; } = 0;

        public double NhapTuNCC { get; set; } = 0;

        public double NhapTuSanXuat { get; set; } = 0;

        public double NhapHaoHut { get; set; } = 0;

        public double NhapChuyenMa { get; set; } = 0;

        public double TongNhap { get; set; } = 0;

        public double XuatTraNCC { get; set; } = 0;

        public double XuatChoNhaMay { get; set; } = 0;

        public double XuatLogistics { get; set; } = 0;

        public double XuatHaoHut { get; set; } = 0;

        public double XuatChuyenMa { get; set; } = 0;

        public double TongXuat { get; set; } = 0;

        public double TonCuoi { get; set; } = 0;

        public double TonAnToan { get; set; } = 0;

        public string LOT { get; set; }

        public string TrangThai { get; set; }

        public string Note { get; set; }
    }
}
