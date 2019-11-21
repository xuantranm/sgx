using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class JobViewModel : PagingExtension
    {
        #region Menu
        public MenuViewModel Menu { get; set; }
        #endregion

        public IList<Job> Entities { get; set; }
        public IList<Link> Links { get; set; }
    }
}
