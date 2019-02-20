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

        

        public IList<EmployeeWorkTimeMonthLog> EmployeeWorkTimeMonthLogs { get; set; }

        public EmployeeWorkTimeMonthLog EmployeeWorkTimeMonthLog { get; set; }

        public DateTime StartWorkingDate { get; set; }

        public DateTime EndWorkingDate { get; set; }

        #region Search
        public string Thang { get; set; }

        // employeeId
        public string Id { get; set; }

        public string Code { get; set; }

        public string Finger { get; set; }

        public string Nl { get; set; }
        #endregion

        public IList<IdName> Approves { get; set; }
        public EmployeeWorkTimeLog EmployeeWorkTimeLog { get; set; }

        #region Extensions
        public int Approve { get; set; }
        public IList<Trainning> ListTraining;
        #endregion 

        public bool Approver { get; set; }

        #region Rights
        public bool RightRequest { get; set; } = false;

        public bool RightManager { get; set; } = false;
        #endregion
    }
}
