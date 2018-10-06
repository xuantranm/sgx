using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class SalaryLogisticViewModel : ExtensionViewModel
    {
        public IList<SalaryLogisticData> SalaryLogistics { get; set; }
    }
}
