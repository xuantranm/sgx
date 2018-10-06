using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class BirthdayViewModel : ExtensionViewModel
    {
        // data display
        public IList<Employee> Employees { get; set; }
    }
}
