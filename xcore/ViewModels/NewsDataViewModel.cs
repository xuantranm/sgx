using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class NewsDataViewModel
    {
        public IList<News> Entities { get; set; }

        public int Code { get; set; }
    }
}
