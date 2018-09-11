using Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ViewModels
{
    public class LeaveSearch: Search
    {
        public string EmployeeId { get; set; }
    }
}
