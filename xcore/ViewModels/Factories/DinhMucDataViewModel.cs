using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System;
using System.Collections.Generic;

namespace ViewModels
{
    public class DinhMucDataViewModel : ExtensionViewModel
    {
        public FactoryDinhMuc Entity { get; set; }

        #region Dropdownlist
        public IList<FactoryStage> Stages { get; set; }
        #endregion
    }
}
