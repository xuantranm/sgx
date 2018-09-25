using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class SalaryEmployeeMonth
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

        public string NoiLamViec { get; set; }

        public string PhongBan { get; set; }

        public string ChucVu { get; set; }

        // base chucvu
        public string ViTriCode { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime ThamNienLamViec { get; set; }

        public int ThamNienYear { get; set; } = 0;

        public int ThamNienMonth { get; set; } = 0;

        public int ThamNienDay { get; set; } = 0;

        public int Bac { get; set; }

        public decimal LuongCanBan { get; set; }

        #region Phu Cap
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NangNhocDocHai { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TrachNhiem { get; set; }

        // Cong thuc: =IF(ThamNienLamViecYear>=3,LuongCanBan*(0.03+(ThamNienLamViecYear-3)*0.01),0)
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThamNien { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuHut { get; set; }
        #endregion

        #region PHUC LOI KHAC

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Xang { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DienThoai { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Com { get; set; }

        // Chua ap dung
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NhaO { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal KiemNhiem { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BhytDacBiet { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ViTriCanKnNhieuNam { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ViTriDacThu { get; set; }
        #endregion

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongCoBanBaoGomPhuCap { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NgayCongLamViec { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NgayNghiPhepHuongLuong { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NgayNghiLeTetHuongLuong { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CongCNGio { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CongTangCaNgayThuongGio { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CongLeTet { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CongTacXa { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal MucDatTrongThang { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongTheoDoanhThuDoanhSo { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TongBunBoc { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThanhTienBunBoc { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongKhac { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThiDua { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal HoTroNgoaiLuong { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TongThuNhap { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BHXHBHYT { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongThamGiaBHXH { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TamUng { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuongLeTet { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucLanh { get; set; }

        // Luong 30 ngay to mau xanh.
        public bool Luong30Ngay { get; set; } = false;


        // sử dụng sau. dùng FlagReal trước
        // muc luong theo luat VN, false theo cong ty
        public bool Law { get; set; } = true;

        // Thuc te - true;
        public bool FlagReal { get; set; } = true;

        public bool Enable { get; set; } = true;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime Start { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedOn { get; set; } = DateTime.Now;

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedOn { get; set; } = DateTime.Now;

    }
}
