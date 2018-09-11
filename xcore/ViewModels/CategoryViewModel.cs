using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class CategoryViewModel : PagingExtension
    {
        #region Menu
        public MenuViewModel Menu { get; set; }
        #endregion

        public ProductCategorySale Entity { get; set; }

        public IList<ProductSale> Entities { get; set; }
        public TextSearch Search { get; set; }
        public IList<Link> Links { get; set; }
    }
}
