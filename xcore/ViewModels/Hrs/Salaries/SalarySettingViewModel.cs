using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class SalarySettingViewModel : ExtensionViewModel
    {
        public IList<SalarySetting> SalarySettings { get; set; }
    }
}
