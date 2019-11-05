using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System;
using System.Collections.Generic;

namespace ViewModels
{
    public class VanHanhViewModel : ExtensionViewModel
    {
        public IList<FactoryVanHanh> FactoryVanHanhs { get; set; }

        public FactoryVanHanh Entity { get; set; }

        public CategoryDisplay Vehicle { get; set; }

        #region Dropdownlist
        public IList<Category> Shifts { get; set; }
        public IList<Category> ShiftSubs { get; set; }
        public IList<Category> Stages { get; set; }
        public IList<Category> Vehicles { get; set; }
        public IList<Category> Products { get; set; }
        public IList<Employee> Employees { get; set; }
        public IList<MonthYear> MonthYears { get; set; }
        #endregion

        #region Search
        public string Ca { get; set; }

        public string CaLamViec { get; set; }

        public string Cv { get; set; }

        public string Cd { get; set; }

        public string Xm { get; set; }

        public string Nvl { get; set; }

        public string Lot { get; set; }

        public string Phieu { get; set; }

        public string Thang { get; set; }

        public DateTime? Tu { get; set; }

        public DateTime? Den { get; set; }

        public int Trang { get; set; }

        public int Dong { get; set; }

        public string Truong { get; set; }

        public string SapXep { get; set; }
        #endregion

        public int Records { get; set; }

        public int Pages { get; set; }
    }
}
