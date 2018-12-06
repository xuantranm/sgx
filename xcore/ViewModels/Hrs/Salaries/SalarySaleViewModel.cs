using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class SalarySaleViewModel : ExtensionViewModel
    {
        public IList<SaleKPI> SaleKPIs { get; set; }
        public IList<SalaryEmployeeMonth> SalaryEmployeeMonths { get; set; }
    }
}
