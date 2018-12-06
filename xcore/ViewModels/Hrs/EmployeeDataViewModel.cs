using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class EmployeeDataViewModel : ExtensionViewModel
    {
        public Employee Employee { get; set; }

        public Employee EmployeeChance { get; set; }

        public bool StatusChange { get; set; }
        // data display
        public IList<Employee> Employees { get; set; }

        public IList<Employee> EmployeesDisable { get; set; }

        public Employee Manager { get; set; }

        #region Salaries
        public IList<NgachLuong> NgachLuongs { get; set; }
        public IList<SalaryThangBangLuong> ThangBangLuongs { get; set; }
        #endregion
    }
}
