using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class TextViewModel : PagingExtension
    {
        public IList<Text> Texts { get; set; }
        public TextSearch Search { get; set; }
    }
}
