using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class EmployeeDataViewModel : ExtensionViewModel
    {
        public Employee Employee { get; set; }

        public Employee EmployeeChance { get; set; }

        public bool StatusChange { get; set; }

        public IList<Employee> Employees { get; set; }

        public IList<Employee> EmployeesDisable { get; set; }

        public Employee Manager { get; set; }

        #region Salaries
        public IList<NgachLuong> NgachLuongs { get; set; }
        public IList<SalaryThangBangLuong> ThangBangLuongs { get; set; }
        #endregion

        #region Email
        // In Edit, Flag Welcome or Edit
        public bool EmailSend { get; set; }

        public bool EmailLeave { get; set; }

        // Store EmailSchedule
        // next data => update EmailSchedule content
        public string EmailGroup { get; set; }

        public string EmailLeaveGroup { get; set; }

        public IList<EmailGroup> EmailGroups { get; set; }

        #endregion

        public IList<CongTyChiNhanh> CongTyChiNhanhs { get; set; }

        public IList<KhoiChucNang> KhoiChucNangs { get; set; }

        public IList<PhongBan> PhongBans { get; set; }

        public IList<BoPhan> BoPhans { get; set; }

        public IList<BoPhan> BoPhanCons { get; set; }

        public IList<ChucVu> ChucVus { get; set; }
    }
}
