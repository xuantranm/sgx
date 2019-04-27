using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class RoleUserViewModel : CommonViewModel
    {
        public IList<RoleUser> RoleUsers { get; set; }

        public IList<Role> Roles { get; set; }

        public Role Role { get; set; }

        public RoleUser RoleUser { get; set; }

        public IList<Employee> Employees { get; set; }

        public IList<CongTyChiNhanh> CongTyChiNhanhs { get; set; }

        public IList<KhoiChucNang> KhoiChucNangs { get; set; }

        public IList<PhongBan> PhongBans { get; set; }

        public IList<BoPhan> BoPhans { get; set; }

        public IList<ChucVu> ChucVus { get; set; }
    }
}
