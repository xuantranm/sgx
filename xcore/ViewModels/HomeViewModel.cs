using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class HomeViewModel
    {
        #region Menu
        public MenuViewModel Menu { get; set; }
        #endregion

        public IList<News> News { get; set; }

        public IList<ExProductSale> Products { get; set; }

        public string LinkBun { get; set; }

        public string LinkDatSach { get; set; }

        public string LinkDichVu { get; set; }
    }
}
