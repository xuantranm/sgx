using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class ProductDataViewModel
    {
        public IList<ProductCategorySale> Categories { get; set; }

        public IList<ProductSale> Entities { get; set; }

        public int Code { get; set; }
    }
}
