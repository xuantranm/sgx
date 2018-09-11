using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class SettingViewModel : ExtensionViewModel
    {
        // data display
        public IList<Setting> Settings { get; set; }

        public IList<Setting> SettingsDisable { get; set; }

        // show title
        public Setting Setting { get; set; }
    }
}
