using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System;
using System.Collections.Generic;

namespace ViewModels
{
    public class DanhGiaXCGDataViewModel : ExtensionViewModel
    {
        public FactoryDanhGiaXCG Entity;

        #region Dropdownlist
        public IList<FactoryStage> Stages { get; set; }
        public IList<FactoryMotorVehicle> Vehicles { get; set; }
        #endregion
    }
}
