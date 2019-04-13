using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class RoleUsageViewModel : CommonViewModel
    {
        public IList<RoleUsage> RoleUsages { get; set; }

        public IList<Role> Roles { get; set; }

        public IList<EmployeeSelectList> EmployeesSL { get; set; }

        public IList<GroupPolicy> GroupPolicies { get; set; }

        public IList<ChucVu> ChucVus { get; set; }

        public RoleUsage RoleUsage { get; set; }

        public string Name { get; set; }

        public string Nhanvien { get; set; }

        public string Chucvu { get; set; }

        public string Nhom { get; set; }
    }
}
