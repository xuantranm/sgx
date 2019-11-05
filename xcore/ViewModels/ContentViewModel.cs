using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class ContentViewModel: CommonViewModel
    {
        public Content Content { get; set; }

        public IList<Content> Contents { get; set; }

        public IList<Category> Categories { get; set; }

        public IList<Setting> Properties { get; set; }

        public IList<CategoryDisplay> CategoriesDisplay { get; set; }

        public string Domain { get; set; }

        public string Category { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }
    }
}
