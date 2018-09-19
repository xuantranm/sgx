using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class TimeKeeperViewModel : ExtensionViewModel
    {
        // data display
        public IList<EmployeeWorkTimeLog> EmployeeWorkTimeLogs { get; set; }

        public IList<Employee> Employees { get; set; }

        public IList<MonthYear> MonthYears { get; set; }

        public Employee Employee { get; set; }

        // information search box
        public TimeKeeperSearch Search { get; set; }

        public IList<EmployeeWorkTimeMonthLog> EmployeeWorkTimeMonthLogs { get; set; }

        public EmployeeWorkTimeMonthLog EmployeeWorkTimeMonthLog { get; set; }

        public DateTime StartWorkingDate { get; set; }

        public DateTime EndWorkingDate { get; set; }

        #region Search
        public string times { get; set; }

        public string employee { get; set; }

        public string code { get; set; }

        public string finger { get; set; }

        public string nl { get; set; }
        #endregion

        #region Request
        public IList<IdName> Approves { get; set; }
        public EmployeeWorkTimeLog EmployeeWorkTimeLog { get; set; }
        #endregion


        #region Extensions
        public int Approve { get; set; }
        public IList<Trainning> ListTraining;
        #endregion 
    }
}
