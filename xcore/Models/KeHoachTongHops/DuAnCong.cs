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
    public class DuAnCong: CommonV101
    {
        public string ThiCongId { get; set; }

        public string GiamSatId { get; set; }

        public string ChuDauTuId { get; set; }

        public string ThiCongAlias { get; set; }

        public string GiamSatAlias { get; set; }

        public string ChuDauTuAlias { get; set; }

        public string ThiCong { get; set; }

        public string GiamSat { get; set; }

        public string ChuDauTu { get; set; }

        [DataType(DataType.Date)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }

        public string BienSoXaLan { get; set; }

        [DataType(DataType.Time)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime GioDi { get; set; }

        [DataType(DataType.Time)]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime GioDen { get; set; }

        //Biên bản/Phiếu vận chuyển có chữ kí & mộc của đv giám sát
        public string BienBan { get; set; }

        // KL thực tế đo đạc tại Đa Phước (m3)
        public double KhoiLuong { get; set; }

        public string HoLuuChua { get; set; }

        public string TenBun { get; set; } // define later, use HoLuuChua

        public string Note { get; set; }
    }
}
