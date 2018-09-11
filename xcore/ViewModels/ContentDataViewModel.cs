using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class ContentDataViewModel
    {
        public IList<Content> Entities { get; set; }

        public string Code { get; set; }
    }
}
