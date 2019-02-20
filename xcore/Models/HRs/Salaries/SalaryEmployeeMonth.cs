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
    public class SalaryEmployeeMonth
    {
        [BsonId]
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

        #region use group, data orginal in Employees

        public int NoiLamViecOrder { get; set; } = 1;

        public int PhongBanOrder { get; set; } = 1;

        public int ChucVuOrder { get; set; } = 1;
        #endregion

        // base chucvu
        public string ViTriCode { get; set; }

        // NgachLuong
        public string SalaryMaSoChucDanhCongViec { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime ThamNienLamViec { get; set; }

        public int ThamNienYear { get; set; } = 0;

        public int ThamNienMonth { get; set; } = 0;

        public int ThamNienDay { get; set; } = 0;

        #region Nha May
        // 3 năm đầu ko tăng, bắt đầu năm thứ 4 sẽ có thâm niên 3%, thêm 1 năm tăng 1%
        public int HeSoThamNien { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongToiThieu { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongCanBanCu { get; set; } = 0;

        // =LuongCanBan/MauSo*NgayCongLamViec
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongDinhMuc { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThanhTienLuongCanBan { get; set; } = 0;

        // Cong thuc
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongVuotDinhMuc { get; set; } = 0;

        // Cong thuc
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PhuCapChuyenCan { get; set; } = 0;

        // Nhap tay
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal PhuCapKhac { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TongPhuCap { get; set; } = 0;

        //Nha May: =ROUND(ThuLanh,-3)+LuongKhac+ComKD+ComSX
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucLanhTronSo { get; set; } = 0;

        #endregion
        // 1. Get direct [Employees]
        // If view History. Use [SalaryEmployeeMonths]
        // No use: [SalaryThangBacLuongEmployees]
        // He So Luong
        public int Bac { get; set; }

        // NhaMay, San Xuat dua vao thang bang luong moi theo Ngach Luong, He So Luong
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongCanBan { get; set; } = 0;

        #region Phu Cap
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal NangNhocDocHai { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TrachNhiem { get; set; } = 0;

        // Cong thuc: =IF(ThamNienLamViecYear>=3,LuongCanBan*(0.03+(ThamNienLamViecYear-3)*0.01),0)
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
        public decimal ComSX { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ComKD { get; set; } = 0;

        // Chua ap dung
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
        public decimal LuongCoBanBaoGomPhuCap { get; set; } = 0;

        public double NgayCongLamViec { get; set; } = 0;

        public double PhutCongLamViec { get; set; } = 0;

        public double NgayNghiPhepNam { get; set; } = 0;

        // Thai san, dam cuoi,...
        public double NgayNghiPhepHuongLuong { get; set; } = 0;

        public double NgayNghiLeTetHuongLuong { get; set; } = 0;

        public double CongCNGio { get; set; } = 0;

        public double CongCNPhut { get; set; } = 0;

        public double CongTangCaNgayThuongGio { get; set; } = 0;

        public double CongTangCaNgayThuongPhut { get; set; } = 0;

        public double CongLeTet { get; set; } = 0;

        public double CongLeTetPhut { get; set; } = 0;

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

        // Ho Tro Them
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongKhac { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThiDua { get; set; } = 0;

        // Ho tro them
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal HoTroNgoaiLuong { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuNhapByMinute { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuNhapByDate { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TongThuNhap { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TongThuNhapMinute { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal LuongThamGiaBHXH { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BHXH { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BHYT { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BHTN { get; set; } = 0;

        // Tong BHXH-BHYT-BHTN
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BHXHBHYT { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal TamUng { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThuongLeTet { get; set; } = 0;

        // NHA MAY: = TONG THU NHAP - VAYTAMUNG - BHXH
        // SAN XUAT:
        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucLanh { get; set; } = 0;

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal ThucLanhMinute { get; set; } = 0;

        public double MauSo { get; set; } = 26;

        // sử dụng sau. dùng FlagReal trước
        // muc luong theo luat VN, false theo cong ty
        public bool Law { get; set; } = true;

        // No use, Law base LuongThamGiaBHXH
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
