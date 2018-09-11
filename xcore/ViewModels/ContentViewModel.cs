using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class ContentViewModel: PagingExtension
    {
        #region Menu
        public MenuViewModel Menu { get; set; }
        #endregion

        public IList<Breadcrumb> Breadcrumbs { get; set; }

        public Content Entity { get; set; }
        public IList<Link> Links { get; set; }
    }
}
