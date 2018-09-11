using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class NewsViewModel : PagingExtension
    {
        #region Menu
        public MenuViewModel Menu { get; set; }
        #endregion

        public IList<News> Entities { get; set; }
        public TextSearch Search { get; set; }
        public IList<Link> Links { get; set; }
    }
}
