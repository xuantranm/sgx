using System;
using System.Collections.Generic;
using System.Text;

namespace ViewModels
{
    public class Menu
    {
        // use for contents
        public string Code { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int? Type { get; set; }
        public int Order { get; set; } = 1;
        public string Language { get; set; }
    }
}
