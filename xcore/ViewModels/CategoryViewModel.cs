﻿using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class CategoryViewModel : CommonViewModel
    {
        public Category Category { get; set; }
        public IList<Category> Categories { get; set; }
        public IList<CategoryDisplay> CategoriesDisplay { get; set; }
        public string Domain { get; set; }
        public string Alias { get; set; }

        public int? Type { get; set; }

        // Merge add/edit. If edit set value into property
        public IList<Property> Properties { get; set; }
    }
}
