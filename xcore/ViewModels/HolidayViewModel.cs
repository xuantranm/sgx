using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class HolidayViewModel
    {
        // data display
        public IList<Holiday> Holidays { get; set; }

        // show title
        public Holiday Holiday { get; set; }
    }
}
