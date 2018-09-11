using Models;
using System;
using System.Collections.Generic;
using ViewModels;

namespace ViewModels
{
    public class RoleViewModel : ExtensionViewModel
    {
        // data display
        public IList<Role> Roles { get; set; }

        public IList<RoleUser> RoleUsers { get; set; }

        public Role Role { get; set; }

        public RoleUser RoleUser { get; set; }
    }
}
