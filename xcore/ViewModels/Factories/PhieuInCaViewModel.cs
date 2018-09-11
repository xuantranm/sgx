using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System;
using System.Collections.Generic;

namespace ViewModels
{
    public class PhieuInCaViewModel : ExtensionViewModel
    {
        public IList<FactoryVanHanh> VanHanhs { get; set; }

        public FactoryNhaThau NhaThau { get; set; }

        #region Dropdownlist
        public IList<FactoryMotorVehicle> Vehicles { get; set; }
        #endregion

        #region Search
        public string xe { get; set; }

        public DateTime? ngay { get; set; }

        public string phieuinca { get; set; }
        #endregion
    }
}
