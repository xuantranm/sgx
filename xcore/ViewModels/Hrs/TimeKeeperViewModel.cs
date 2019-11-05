using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class TimeKeeperViewModel : CommonViewModel
    {
        public bool IsMe { get; set; }
        // data display
        public IList<EmployeeWorkTimeLog> EmployeeWorkTimeLogs { get; set; }

        public IList<TimeKeeperDisplay> TimeKeeperDisplays { get; set; }

        public IList<Employee> Employees { get; set; }

        public IList<Category> CongTyChiNhanhs { get; set; }

        public IList<Category> KhoiChucNangs { get; set; }

        public IList<Category> PhongBans { get; set; }

        public IList<Category> BoPhans { get; set; }

        public IList<Category> ChucVus { get; set; }

        public IList<MonthYear> MonthYears { get; set; }

        public Employee Employee { get; set; }

        public IList<EmployeeWorkTimeMonthLog> EmployeeWorkTimeMonthLogs { get; set; }

        public EmployeeWorkTimeMonthLog EmployeeWorkTimeMonthLog { get; set; }

        public DateTime StartWorkingDate { get; set; }

        public DateTime EndWorkingDate { get; set; }

        #region Search
        public string Thang { get; set; }

        public DateTime Tu { get; set; }

        public DateTime Den { get; set; }

        // employeeId
        public string Id { get; set; }

        public string Code { get; set; }

        public string Fg { get; set; }

        public string Nl { get; set; }

        public string Kcn { get; set; }

        public string Pb { get; set; }

        public string Bp { get; set; }
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

        public bool IsManager { get; set; } = false;

        public bool IsSecurity { get; set; } = false;
        #endregion

        public bool Error { get; set; } = false;

        public string Name { get; set; }

        #region Tao Lenh Tang Ca
        public int TrangThai { get; set; }

        public int CodeInt { get; set; }

        public IList<OvertimeEmployee> OvertimeEmployees { get; set; }

        public OvertimeEmployee OvertimeEmployee { get; set; }
        #endregion

        public Employee Manager { get; set; }
    }
}
