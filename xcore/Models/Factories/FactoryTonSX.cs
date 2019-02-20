using Common.Utilities;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class FactoryTonSX: Common
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; } = 0;

        public int Month { get; set; } = 0;

        public int Week { get; set; } = 0;

        public int Day { get; set; } = 0;

        [Display(Name ="Ngày")]
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Date { get; set; }

        public string ProductId { get; set; }

        //Tên NVL/BTP/TP
        [Display(Name = "Tên NVL/BTP/TP")]
        public string Product { get; set; }

        public string ProductAlias { get; set; }

        [Display(Name = "ĐVT")]
        public string Unit { get; set; }

        [Display(Name = "LOT")]
        public string LOT { get; set; }

        [Display(Name = "Tồn đầu ngày")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TonDauNgay { get; set; } = 0;

        [Display(Name = "Nhập từ sản xuất")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NhapTuSanXuat { get; set; } = 0;

        [Display(Name = "Xuất cho sản xuất")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XuatChoSanXuat { get; set; } = 0;

        [Display(Name = "Nhập từ kho")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NhapTuKho { get; set; } = 0;

        [Display(Name = "Xuất cho kho")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XuatChoKho { get; set; } = 0;

        [Display(Name = "Xuất hao hụt")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal XuatHaoHut { get; set; } = 0;

        [Display(Name = "Tái xuất")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TaiXuat { get; set; } = 0;

        [Display(Name = "Tái nhập")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TaiNhap { get; set; } = 0;

        // Big number
        [Display(Name = "Tồn cuối ngày")]
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TonCuoiNgay { get; set; } = 0;
    }
}
