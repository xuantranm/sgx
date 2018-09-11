using Models;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ViewModels
{
    public class EmployeeSearch: Search
    {
        // UserName or Email
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        [Display(Name="Mã nhân viên")]
        public string Code { get; set; }

        [Display(Name = "Tên")]
        public string Alias { get; set; }
        
        public IList<Part> Parts { get; set; }

        [Display(Name = "Phòng/ban")]
        public string Part { get; set; }

        [Display(Name = "Bộ phận")]
        public IList<Department> Departments { get; set; }

        public string Department { get; set; }

        [Display(Name = "Số cmnd/hộ chiếu")]
        public string IdentityCard { get; set; }

        [Display(Name = "Số xổ bhxh")]
        public string BhxhBookNo { get; set; }

        [Display(Name = "Điện thoại")]
        public string Phone { get; set; }
    }
}
