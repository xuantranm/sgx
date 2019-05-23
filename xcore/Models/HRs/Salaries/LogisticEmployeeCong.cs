using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// 
    /// </summary>
    public class LogisticEmployeeCong
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public string EmployeeId { get; set; }

        public string MaNhanVien { get; set; }

        public string FullName { get; set; }

        public string ChucVu { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DoanhThu { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongTheoDoanhThuDoanhSo { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen1HcmXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen2HcmXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen3HcmXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen4HcmXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen5HcmXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen1HcmXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen2HcmXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen3HcmXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen4HcmXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen5HcmXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen1BinhDuongXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen2BinhDuongXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen3BinhDuongXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen4BinhDuongXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen5BinhDuongXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen1BinhDuongXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen2BinhDuongXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen3BinhDuongXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen4BinhDuongXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen5BinhDuongXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen1BienHoaXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen2BienHoaXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen3BienHoaXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen4BienHoaXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen5BienHoaXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen1BienHoaXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen2BienHoaXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen3BienHoaXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen4BienHoaXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Chuyen5BienHoaXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal VungTauXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal VungTauXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BinhThuanXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BinhThuanXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CanThoXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal VinhLongXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LongAnXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LongAnXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TienGiangXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TienGiangXeLon { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DongNaiXeNho { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DongNaiXeLon { get; set; } = 0;

        public decimal TongSoChuyen { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TienChuyen { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CongTacXa { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public double KhoiLuongBun { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThanhTienBun { get; set; }

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;
    }
}
