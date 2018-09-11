using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System;
using System.Collections.Generic;

namespace ViewModels
{
    public class VanHanhDataViewModel : ExtensionViewModel
    {
        public FactoryVanHanh Entity;

        #region Dropdownlist
        public IList<FactoryWork> Works { get; set; }
        public IList<FactoryStage> Stages { get; set; }
        public IList<FactoryMotorVehicle> Vehicles { get; set; }
        public IList<FactoryProduct> Products { get; set; }
        #endregion
    }
}
