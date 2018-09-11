using Models;
using System.Collections.Generic;

namespace ViewModels
{
    public class JobDataViewModel
    {
        public IList<Job> Entities { get; set; }

        public string Code { get; set; }
    }
}
