using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class ProductViewModel
    {
        #region Menu
        public MenuViewModel Menu { get; set; }
        #endregion

        public IList<Breadcrumb> Breadcrumbs { get; set; }

        public ProductSale Entity { get; set; }

        public IList<ProductSale> Relations { get; set; }

        public IList<Link> Links { get; set; }
    }
}
