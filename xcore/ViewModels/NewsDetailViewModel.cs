using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class NewsDetailViewModel : PagingExtension
    {
        #region Menu
        public MenuViewModel Menu { get; set; }
        #endregion

        public IList<Breadcrumb> Breadcrumbs { get; set; }

        public News Entity { get; set; }

        public IList<News> Relations { get; set; }

        public IList<Link> Links { get; set; }
    }
}
