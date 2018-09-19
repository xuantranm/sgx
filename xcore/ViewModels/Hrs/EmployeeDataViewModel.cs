using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class EmployeeDataViewModel : ExtensionViewModel
    {
        public Employee Employee { get; set; }

        public IList<Variance> Variances { get; set; }
        // data display
        public IList<Employee> Employees { get; set; }

        public IList<Employee> EmployeesDisable { get; set; }

        public Employee Manager { get; set; }
    }
}
