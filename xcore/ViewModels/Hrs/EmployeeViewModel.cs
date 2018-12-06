using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class EmployeeViewModel : ExtensionViewModel
    {
        // data display
        public IList<Employee> Employees { get; set; }

        // base on Employees
        public IList<Department> Departments { get; set; }

        public IList<Part> Parts { get; set; }

        public IList<Employee> EmployeesDisable { get; set; }

        // show title
        public Employee Employee { get; set; }

        public IList<Employee> EmployeesDdl { get; set; }

        #region Search
        public string id { get; set; }

        public string ten { get; set; }

        public string code { get; set; }

        public string finger { get; set; }

        public string nl { get; set; }

        public int page { get; set; }

        public int size { get; set; }

        public string sortBy { get; set; }

        public string sort { get; set; }
        #endregion

        public int Records { get; set; }

        public int Pages { get; set; }
    }
}
