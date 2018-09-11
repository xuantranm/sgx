using System.Collections.Generic;

namespace ViewModels
{
    public class MenuViewModel
    {
        // Use common for product, xu-ly-tai-che, dich-vu
        public IList<Menu> MenusProduct { get; set; }
        public IList<Menu> MenusProccess { get; set; }
        public IList<Menu> MenusService { get; set; }
        public IList<Menu> MenusContent { get; set; }
    }
}
