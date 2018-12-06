using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Tham chiếu từng tháng với SaleKPIs. Tháng nào ra tháng đó
    /// Bảng lương sử dụng SaleKPIEmployee thì qui định trong [SalaryEmployeeMonths] : YearSale và MonthSale
    /// </summary>
    public class SaleKPIEmployee
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
        public decimal ChiTieuDoanhSo { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuDoanhThu { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuDoPhu { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuMoMoi { get; set; } = 0;

        public int ChiTieuNganhHang { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucHienDoanhSo { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucHienDoanhThu { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucHienDoPhu { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucHienMoMoi { get; set; } = 0;

        public int ThucHienNganhHang { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuThucHienDoanhSo { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuThucHienDoanhThu { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuThucHienDoPhu { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ChiTieuThucHienMoMoi { get; set; } = 0;

        public int ChiTieuThucHienNganhHang { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuongChiTieuThucHienDoanhSo { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuongChiTieuThucHienDoanhThu { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuongChiTieuThucHienDoPhu { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuongChiTieuThucHienMoMoi { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuongChiTieuThucHienNganhHang { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TongThuong { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuViec { get; set; } = 0;

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}
