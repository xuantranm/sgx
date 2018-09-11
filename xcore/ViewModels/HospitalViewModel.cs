using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class HospitalViewModel : ExtensionViewModel
    {
        // data display
        public IList<BHYTHospital> Hospitals { get; set; }

        // show title
        public BHYTHospital Hospital { get; set; }
    }
}
