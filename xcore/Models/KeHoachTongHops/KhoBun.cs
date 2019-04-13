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
    public class KhoBun: CommonV101
    {
        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }

        public string ProductId { get; set; }

        public string ProductName { get; set; }

        public string ProductAlias { get; set; }

        public string ProductCode { get; set; }

        public string ProductUnit { get; set; }

        public string CustomerId { get; set; }

        public string Customer { get; set; }

        public string CustomerAlias { get; set; }

        public string HoChua { get; set; }

        public string HoChuaAlias { get; set; }

        public string DVT { get; set; }

        public double TonDau { get; set; } = 0;

        public double NhapKho { get; set; } = 0;

        public double XuLy { get; set; } = 0;

        public double XuLyBao { get; set; } = 0;

        public double XuLyKhac { get; set; } = 0;

        public double HaoHut { get; set; } = 0;

        public double TonKho { get; set; } = 0;

        public string TrangThai { get; set; }

        public string Note { get; set; }

        public int PhanLoaiDuAn { get; set; } = (int)EBun.DAC;
    }
}
