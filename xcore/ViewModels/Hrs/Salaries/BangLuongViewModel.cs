using Common.Utilities;
using Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class BangLuongViewModel : CommonViewModel
    {
        public IList<SalaryEmployeeMonth> Salaries { get; set; }

        public SalaryEmployeeMonth Salary { get; set; }

        public SalaryMucLuongVung SalaryMucLuongVung { get; set; }

        public NgachLuong NgachLuong { get; set; }

        public IList<NgachLuong> NgachLuongs { get; set; }

        public IList<SalaryThangBangPhuCapPhucLoi> SalaryThangBangPhuCapPhucLois { get; set; }

        public IList<SalaryThangBangPhuCapPhucLoi> SalaryThangBangPhuCapPhucLoisReal { get; set; }

        public string Thang { get; set; }

        public string SaleTimes { get; set; }

        public string LogisticTimes { get; set; }

        public bool CalBHXH { get; set; }

        public IList<MonthYear> MonthYears { get; set; }

        public IList<SalaryDuration> SalaryDurations { get; set; }

        public SalaryDuration SalaryDuration { get; set; }

        public IList<SaleKPI> SaleKPIs { get; set; }

        public IList<SaleKPIEmployee> SaleKPIEmployees { get; set; }

        public SaleKPIEmployee SaleKPIEmployee { get; set; }

        public IList<CreditEmployee> Credits { get; set; }

        public IList<LogisticGiaChuyenXe> LogisticGiaChuyenXes { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DonGiaBun { get; set; }

        public IList<LogisticEmployeeCong> LogisticEmployeeCongs { get; set; }

        public LogisticEmployeeCong LogisticEmployeeCong { get; set; }

        public IList<EmployeeWorkTimeLog> EmployeeWorkTimeLogs { get; set; }

        public IList<Employee> Employees { get; set; }

        public string Id { get; set; }

        public string Manv { get; set; }

        public string Ct { get; set; }

        public string Kcn { get; set; }

        public string Pb { get; set; }

        public string Bp { get; set; }

        public string Cv { get; set; }

        public IList<Category> CongTyChiNhanhs { get; set; }

        public IList<Category> KhoiChucNangs { get; set; }

        public IList<Category> PhongBans { get; set; }

        public IList<Category> BoPhans { get; set; }

        public IList<Category> ChucVus { get; set; }

        public DateTime Tu { get; set; }

        public DateTime Den { get; set; }

        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public IList<EmployeeCong> Congs { get; set; }

        #region SAN XUAT
        public IList<FactoryProductCongTheoThang> MCongs { get; set; }

        public IList<FactoryProduct> ThanhPhams { get; set; }

        public IList<FactoryCongViec> CongViecs { get; set; }

        public IList<FactoryProductDinhMuc> DonGiaDMs { get; set; }

        public IList<FactoryProductDinhMucTiLe> TiLeDMs { get; set; }

        public FactoryProductDonGiaM3 DonGiaM3 { get; set; }

        public IList<FactoryProductDinhMucViewModel> DonGiaDMFulls { get; set; }
        #endregion

        public int ThamSo { get; set; }
    }
}
