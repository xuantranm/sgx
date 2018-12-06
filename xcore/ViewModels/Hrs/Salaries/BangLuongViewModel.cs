using Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class BangLuongViewModel : ExtensionViewModel
    {
        public IList<SalaryEmployeeMonth> SalaryEmployeeMonths { get; set; }

        public IList<SalaryThangBacLuongEmployee> SalaryThangBacLuongEmployees { get; set; }

        public SalaryMucLuongVung SalaryMucLuongVung { get; set; }

        public IList<SalaryThangBangLuong> SalaryThangBangLuongLaws { get; set; }

        public IList<SalaryThangBangLuong> SalaryThangBangLuongReals { get; set; }

        public IList<SalaryThangBangPhuCapPhucLoi> SalaryThangBangPhuCapPhucLois { get; set; }

        public IList<SalaryThangBangPhuCapPhucLoi> SalaryThangBangPhuCapPhucLoisReal { get; set; }

        public string thang { get; set; }

        public string saleTimes { get; set; }

        public string logisticTimes { get; set; }

        public bool calBHXH { get; set; }

        public IList<MonthYear> MonthYears { get; set; }

        public IList<SalarySetting> SalarySettings { get; set; }

        public IList<SaleKPI> SaleKPIs { get; set; }

        public IList<SaleKPIEmployee> SaleKPIEmployees { get; set; }

        public IList<SalaryCredit> SalaryCredits { get; set; }

        public IList<LogisticGiaChuyenXe> LogisticGiaChuyenXes { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal DonGiaBun { get; set; }

        public IList<LogisticEmployeeCong> LogisticEmployeeCongs { get; set; }

        public IList<EmployeeWorkTimeLog> EmployeeWorkTimeLogs { get; set; }

        public IList<Employee> Employees { get; set; }

        // EmployeeId / Id /...
        public string Id { get; set; }

        public string Manv { get; set; }

        public IList<Department> Departments { get; set; }

        public string phongban { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        // NHA MAY
        public IList<SalaryNhaMayCong> SalaryNhaMayCongs { get; set; }

        public int Records { get; set; }

        public int Pages { get; set; }

        public IList<Employee> EmployeesDdl { get; set; }
    }
}
