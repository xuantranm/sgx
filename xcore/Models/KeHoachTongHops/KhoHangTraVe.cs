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
    public class KhoHangTraVe: CommonV101
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

        public double NhapTuKhoThanhPham { get; set; } = 0;

        public double NhapTuKinhDoanh { get; set; } = 0;

        public double XuatChuyenKhoThanhPham { get; set; } = 0;

        public double XuatXuLy { get; set; } = 0;

        public double TonCuoi { get; set; } = 0;

        public double TonAnToan { get; set; } = 0;

        public string TrangThai { get; set; }

        public string Note { get; set; }
    }
}
