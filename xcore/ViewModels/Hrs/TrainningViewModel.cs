using Microsoft.AspNetCore.Mvc.Rendering;
using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class TrainningViewModel : ExtensionViewModel
    {
        public IList<Trainning> List;

        #region Dropdownlist
        public SelectList Types;
        #endregion

        #region Search
        public string search { get; set; }

        public string type { get; set; }

        public int page { get; set; }

        public int size { get; set; }

        public string sortField { get; set; }

        public string sort { get; set; }
        #endregion

        public int Records { get; set; }

        public int Pages { get; set; }
    }
}
