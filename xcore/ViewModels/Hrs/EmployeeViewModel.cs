using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class EmployeeViewModel : ExtensionViewModel
    {
        public IList<Employee> Employees { get; set; }

        public Employee Employee { get; set; }

        public bool StatusChange { get; set; }

        public Employee EmployeeChance { get; set; }

        public IList<CongTyChiNhanh> CongTyChiNhanhs { get; set; }

        public IList<KhoiChucNang> KhoiChucNangs { get; set; }

        public IList<PhongBan> PhongBans { get; set; }

        public IList<PhongBanBoPhanDisplay> CoCaus { get; set; }

        public IList<BoPhan> BoPhans { get; set; }

        public IList<BoPhan> BoPhanCons { get; set; }

        public IList<ChucVu> ChucVus { get; set; }

        public IList<Employee> EmployeesDdl { get; set; }

        public IList<WorkTimeType> WorkTimeTypes { get; set; }

        public IList<BHYTHospital> Hospitals { get; set; }

        public IList<ContractType> Contracts { get; set; }

        //public IList<WorkTimeType> WorkTimeTypes { get; set; }

        public IList<NgachLuong> NgachLuongs { get; set; }

        #region Search
        public string Id { get; set; }

        public string Ten { get; set; }

        public string Code { get; set; }

        public string Fg { get; set; }

        public string Nl { get; set; }

        public string Kcn { get; set; }

        public string Pb { get; set; }

        public string PbBp { get; set; }

        public string Bp { get; set; }

        public int Page { get; set; }

        public int Size { get; set; }

        public string Sortby { get; set; }

        public string Sort { get; set; }

        public string LinkCurrent { get; set; }
        #endregion

        public Employee Manager { get; set; }

        // Flag
        public bool IsWelcomeEmail { get; set; } = false;

        public bool IsLeaveEmail { get; set; } = false;

        public string WelcomeEmailGroup { get; set; }

        public string LeaveEmailGroup { get; set; }

        // Welcome
        public string WelcomeOtherEmail { get; set; } // Customer email send.

        public string LeaveOtherEmail { get; set; } // Customer email send.

        public bool WelcomeEmailAll { get; set; }

        public bool LeaveEmailAll { get; set; }

        public int RecordCurrent { get; set; }

        public int RecordLeave { get; set; }

        public int Records { get; set; }

        public int Pages { get; set; }
    }
}
