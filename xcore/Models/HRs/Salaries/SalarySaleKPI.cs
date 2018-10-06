using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// 
    /// </summary>
    public class SalarySaleKPI
    {
        [BsonId]
        // Mvc don't know how to create ObjectId from string
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public int Year { get; set; }

        public int Month { get; set; }

        public string EmployeeId { get; set; }

        public string MaNhanVien { get; set; }

        public string FullName { get; set; }

        public string ChucVu { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuDoanhSo { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuDoanhThu { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuDoPhu { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuMoMoi { get; set; }

        public int ChiTieuNganhHang { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucHienDoanhSo { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucHienDoanhThu { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucHienDoPhu { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucHienMoMoi { get; set; }

        public int ThucHienNganhHang { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuThucHienDoanhSo { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuThucHienDoanhThu { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuThucHienDoPhu { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuThucHienMoMoi { get; set; }

        public int ChiTieuThucHienNganhHang { get; set; }


        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuongChiTieuThucHienDoanhSo { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuongChiTieuThucHienDoanhThu { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuongChiTieuThucHienDoPhu { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuongChiTieuThucHienMoMoi { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuongChiTieuThucHienNganhHang { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TongThuong { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuViec { get; set; }

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}
