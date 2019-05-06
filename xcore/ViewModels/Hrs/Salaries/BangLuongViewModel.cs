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
        public IList<SalaryEmployeeMonth> SalaryEmployeeMonths { get; set; }

        public SalaryMucLuongVung SalaryMucLuongVung { get; set; }

        public IList<SalaryThangBangLuong> SalaryThangBangLuongLaws { get; set; }

        public IList<SalaryThangBangLuong> SalaryThangBangLuongReals { get; set; }

        public IList<SalaryThangBangPhuCapPhucLoi> SalaryThangBangPhuCapPhucLois { get; set; }

        public IList<SalaryThangBangPhuCapPhucLoi> SalaryThangBangPhuCapPhucLoisReal { get; set; }

        public string Thang { get; set; }

        public string SaleTimes { get; set; }

        public string LogisticTimes { get; set; }

        public bool CalBHXH { get; set; }

        public IList<MonthYear> MonthYears { get; set; }

        public IList<SaleKPI> SaleKPIs { get; set; }

        public IList<SaleKPIEmployee> SaleKPIEmployees { get; set; }

        public IList<CreditEmployee> Credits { get; set; }

        public IList<LogisticGiaChuyenXe> LogisticGiaChuyenXes { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DonGiaBun { get; set; }

        public IList<LogisticEmployeeCong> LogisticEmployeeCongs { get; set; }

        public IList<EmployeeWorkTimeLog> EmployeeWorkTimeLogs { get; set; }

        public IList<Employee> Employees { get; set; }

        public string Id { get; set; }

        public string Manv { get; set; }

        public string KhoiChucNang { get; set; }

        public string Phongban { get; set; }

        public string BoPhan { get; set; }

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

        public int ThamSoTinhLuong { get; set; }
    }
}
