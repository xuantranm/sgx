using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class NewsDataViewModel
    {
        public News Entity { get; set; }

        public int Code { get; set; }

        public string Language { get; set; }

        public IList<News> Entities { get; set; }

        public IList<Language> Languages { get; set; }

        public IList<NewsCategory> Categories { get; set; }
    }
}
