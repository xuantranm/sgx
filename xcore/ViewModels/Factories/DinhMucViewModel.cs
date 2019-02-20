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
        public string Ca { get; set; }

        public string Cv { get; set; }

        public string Cd { get; set; }

        public string Xm { get; set; }

        public string Nvl { get; set; }

        public string Lot { get; set; }

        public int Page { get; set; }

        public int Size { get; set; }

        public string SortField { get; set; }

        public string Sort { get; set; }
        #endregion

        public int Records { get; set; }

        public int Pages { get; set; }
    }
}
