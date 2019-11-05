using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class TextViewModel : CommonViewModel
    {
        public Text Text { get; set; }
        public IList<Text> Texts { get; set; }
        public string Domain { get; set; }
        public int? Code { get; set; }
    }
}
