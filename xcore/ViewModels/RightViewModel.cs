using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class RightViewModel : CommonViewModel
    {
        public Right Right { get; set; }
        public IList<Right> Rights { get; set; }
        public IList<RightDisplay> RightsDisplay { get; set; }
        public IList<Category> Categories { get; set; }
        public string Role { get; set; }

        public string Ob { get; set; }
    }
}
