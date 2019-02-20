using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class SalaryCreditViewModel : ExtensionViewModel
    {
        public IList<CreditEmployee> SalaryCredits { get; set; }
    }
}
