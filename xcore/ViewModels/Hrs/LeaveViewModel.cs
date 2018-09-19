using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class LeaveViewModel : ExtensionViewModel
    {
        public IList<Leave> Leaves { get; set; }

        // Quản lý số ngày nghỉ phép còn lại (nghỉ phép, các loại nghỉ bù,...)
        public IList<LeaveEmployee> LeaveEmployees { get; set; }

        public Employee Employee { get; set; }

        public bool RightRequest { get; set; } = false;

        #region Extensions
        public int Approve { get; set; }
        public IList<Trainning> ListTraining;
        #endregion  

        #region Search
        public string cd { get; set; }

        public string xm { get; set; }

        public string rate { get; set; }
        #endregion

        #region Create
        public Leave Leave { get; set; }

        public IList<IdName> Approves { get; set; }

        public IList<LeaveType> Types { get; set; }

        public IList<Employee> Employees { get; set; }
        #endregion
    }
}
