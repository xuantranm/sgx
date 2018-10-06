using Models;
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

        public bool calBHXH { get; set; }

        public IList<MonthYear> MonthYears { get; set; }

        public IList<SalarySetting> SalarySettings { get; set; }

        public IList<SaleKPI> SaleKPIs { get; set; }

        public IList<SalarySaleKPI> SalarySaleKPIs { get; set; }

        public IList<SalaryCredit> SalaryCredits { get; set; }

        public IList<SalaryLogisticData> SalaryLogisticDatas { get; set; }
    }
}
