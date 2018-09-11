using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System;
using System.Collections.Generic;

namespace ViewModels
{
    public class ChiPhiXCGViewModel : ExtensionViewModel
    {
        public IList<FactoryChiPhiXCG> List { get; set; }

        #region Dropdownlist
        public IList<FactoryMotorVehicle> Vehicles { get; set; }
        #endregion

        #region Search
        public string xcg { get; set; }

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
