using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Quan ly BHXH moi thang.
    /// Thuong le tet moi thang
    /// </summary>
    public class SalaryEmployeeMonth: CommonV101
    {
        public int Year { get; set; }

        public int Month { get; set; }

        #region THONG TIN NHAN VIEN
        public string EmployeeId { get; set; }

        public string EmployeeCode { get; set; } // current

        public string EmployeeFullName { get; set; }

        public string CongTyChiNhanhId { get; set; }

        public string CongTyChiNhanhName { get; set; }

        public string KhoiChucNangId { get; set; }

        public string KhoiChucNangName { get; set; }

        public string PhongBanId { get; set; }

        public string PhongBanName { get; set; }

        public string BoPhanId { get; set; }

        public string BoPhanName { get; set; }

        public string BoPhanConId { get; set; }

        public string BoPhanConName { get; set; }

        public string ChucVuId { get; set; }

        public string ChucVuName { get; set; }

        public string NgachLuongCode { get; set; }

        public int NgachLuongLevel { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime JoinDate { get; set; }

        public double MauSo { get; set; } = 26;
        #endregion

        #region THONG TIN LUONG
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongToiThieuVung { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongCanBan { get; set; } = 0;

        // = LuongCanBan/MauSo*NgayCongLamViec
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongDinhMuc { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThanhTienLuongCanBan { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongVuotDinhMuc { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PhuCapChuyenCan { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PhuCapKhac { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TongPhuCap { get; set; } = 0;

        //Nha May: =ROUND(ThuLanh,-3)+LuongKhac+ComKD+ComSX
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucLanhTronSo { get; set; } = 0;

        #region Phu Cap
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NangNhocDocHai { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TrachNhiem { get; set; } = 0;

        // 3 năm đầu ko tăng, bắt đầu năm thứ 4 sẽ có thâm niên 3%, thêm 1 năm tăng 1%
        public int HeSoThamNien { get; set; } = 0;

        public int ThamNienYear { get; set; } = 0;

        public int ThamNienMonth { get; set; } = 0;

        public int ThamNienDay { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThamNien { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuHut { get; set; } = 0;
        #endregion

        #region PHUC LOI KHAC
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Xang { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DienThoai { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Com { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NhaO { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal KiemNhiem { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BhytDacBiet { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ViTriCanKnNhieuNam { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ViTriDacThu { get; set; } = 0;
        #endregion

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ComSX { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ComNM { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ComKD { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ComVP { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongCoBanBaoGomPhuCap { get; set; } = 0;

        public double NgayCongLamViec { get; set; } = 0;

        public double NgayNghiPhepNam { get; set; } = 0;

        public double NgayNghiPhepHuongLuong { get; set; } = 0;// Thai san, dam cuoi,...

        public double NgayNghiLeTetHuongLuong { get; set; } = 0;

        public double CongCNGio { get; set; } = 0;

        public double CongTangCaNgayThuongGio { get; set; } = 0;

        public double CongLeTet { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TienPhepNamLeTet { get; set; } = 0;

        public int YearLogistic { get; set; } = DateTime.Now.Year;

        public int MonthLogistic { get; set; } = DateTime.Now.Month;

        public int YearSale { get; set; } = DateTime.Now.Year;

        public int MonthSale { get; set; } = DateTime.Now.Month;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal CongTacXa { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal MucDatTrongThang { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongTheoDoanhThuDoanhSo { get; set; } = 0;

        public double TongBunBoc { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThanhTienBunBoc { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongKhac { get; set; } = 0; // Ho Tro Them

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThiDua { get; set; } = 0;
        
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal HoTroNgoaiLuong { get; set; } = 0; // Phu cap khac

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuNhap { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TongThuNhap { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongThamGiaBHXH { get; set; } = 0; // IN THIS COLECTION. First get Employees

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BHXH { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BHYT { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BHTN { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BHXHBHYT { get; set; } = 0; // Tong BHXH-BHYT-BHTN

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TamUng { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuongLeTet { get; set; } = 0; // IN THIS COLECTION

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucLanh { get; set; } = 0;
        #endregion

        public bool Law { get; set; } = true;
    }
}
