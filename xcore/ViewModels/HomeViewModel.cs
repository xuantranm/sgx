using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class HomeViewModel: CommonViewModel
    {
        public Category Category { get; set; }

        public Content Content { get; set; }

        public IList<Content> Contents { get; set; }

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
