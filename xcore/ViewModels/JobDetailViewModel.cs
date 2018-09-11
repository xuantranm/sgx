using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class JobDetailViewModel : PagingExtension
    {
        #region Menu
        public MenuViewModel Menu { get; set; }
        #endregion

        public IList<Breadcrumb> Breadcrumbs { get; set; }

        public Job Entity { get; set; }
        public IList<Link> Links { get; set; }
    }
}
