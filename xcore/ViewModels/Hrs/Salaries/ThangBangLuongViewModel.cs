﻿using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class ThangBangLuongViewModel : ExtensionViewModel
    {
        public SalaryMucLuongVung SalaryMucLuongVung { get; set; }

        public IList<NgachLuong> NgachLuongs { get; set; }

        public IList<NgachLuong> SalaryThangBangLuongs { get; set; }

        public IList<SalaryThangBangPhuCapPhucLoi> SalaryThangBangPhuCapPhucLois { get; set; }

        public IList<SalaryThangBangPhuCapPhucLoi> SalaryThangBangPhuCapPhucLoisReal { get; set; }

        public string Thang { get; set; }

        public IList<MonthYear> MonthYears { get; set; }
    }
}
