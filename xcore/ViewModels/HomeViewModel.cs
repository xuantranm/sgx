using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class HomeViewModel
    {
        #region Menu
        public MenuViewModel Menu { get; set; }
        #endregion

        public IList<News> News { get; set; }
    }
}
