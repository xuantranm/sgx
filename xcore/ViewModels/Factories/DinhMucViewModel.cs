using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System;
using System.Collections.Generic;

namespace ViewModels
{
    public class DinhMucViewModel : ExtensionViewModel
    {
        public IList<FactoryDinhMuc> List { get; set; }

        #region Dropdownlist
        public IList<FactoryStage> Stages { get; set; }
        #endregion

        #region Search
        public string ca { get; set; }

        public string cv { get; set; }

        public string cd { get; set; }

        public string xm { get; set; }

        public string nvl { get; set; }

        public string lot { get; set; }

        public DateTime? from { get; set; }

        public DateTime? to { get; set; }

        public int page { get; set; }

        public int size { get; set; }

        public string sortField { get; set; }

        public string sort { get; set; }
        #endregion

        public int Records { get; set; }

        public int Pages { get; set; }
    }
}
