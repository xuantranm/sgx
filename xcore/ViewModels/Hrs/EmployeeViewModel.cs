using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class EmployeeViewModel : ExtensionViewModel
    {
        public IList<EmployeeDisplay> Employees { get; set; }

        public EmployeeDisplay EmployeeDetail { get; set; }

        public bool StatusChange { get; set; }

        public Employee EmployeeChance { get; set; }

        public IList<CongTyChiNhanh> CongTyChiNhanhs { get; set; }

        public IList<KhoiChucNang> KhoiChucNangs { get; set; }

        public IList<PhongBan> PhongBans { get; set; }

        public IList<BoPhan> BoPhans { get; set; }

        public IList<ChucVu> ChucVus { get; set; }

        public IList<Employee> EmployeesDdl { get; set; }

        #region Search
        public string Id { get; set; }

        public string Ten { get; set; }

        public string Code { get; set; }

        public string Fg { get; set; }

        public string Nl { get; set; }

        public string Kcn { get; set; }

        public string Pb { get; set; }

        public string Bp { get; set; }

        public int Page { get; set; }

        public int Size { get; set; }

        public string Sortby { get; set; }

        public string Sort { get; set; }

        public string LinkCurrent { get; set; }
        #endregion

        public int RecordCurrent { get; set; }

        public int RecordLeave { get; set; }

        public int Records { get; set; }

        public int Pages { get; set; }
    }
}
