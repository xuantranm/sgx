using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class SettingViewModel : CommonViewModel
    {
        public Setting Setting { get; set; }
        public IList<Setting> Settings { get; set; }
        public string Domain { get; set; }
        public string Key { get; set; }

        public int? Type { get; set; }
    }
}
