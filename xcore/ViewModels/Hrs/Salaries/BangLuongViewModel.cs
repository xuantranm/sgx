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

        public IList<SalaryThangBangLuong> SalaryThangBangLuongs { get; set; }

        public IList<SalaryThangBangPhuCapPhucLoi> SalaryThangBangPhuCapPhucLois { get; set; }

        public IList<SalaryThangBangPhuCapPhucLoi> SalaryThangBangPhuCapPhucLoisReal { get; set; }
    }
}
