﻿using Common.Enums;
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
    public class TiepNhanXuLy: CommonV101
    {
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }

        public string SoPhieu { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime NgayGioL1 { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime NgayGioL2 { get; set; }

        public string BienSo1 { get; set; }

        public string BienSo2 { get; set; }

        public double TrongLuongL1 { get; set; } = 0;

        public double TrongLuongL2 { get; set; } = 0;

        public double TrongLuong { get; set; } = 0;

        public string CustomerId { get; set; }

        public string Customer { get; set; }

        public string CustomerChildId { get; set; }

        public string CustomerChild { get; set; }

        public string DonViVanChuyen { get; set; }

        public string LoaiHang { get; set; }

        public string KhoHang { get; set; }

        public int NhapXuat { get; set; } = (int)ETiepNhanXuLy.Nhap;

        public string NguoiCan { get; set; }

        [DataType(DataType.Time)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime GioCan1 { get; set; }

        [DataType(DataType.Time)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime GioCan2 { get; set; }

        public string PhanLoai { get; set; }

        public string Note { get; set; }
    }
}