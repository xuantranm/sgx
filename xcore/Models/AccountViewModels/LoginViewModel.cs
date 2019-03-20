using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class LoginViewModel
    {
        //[Required]
        //[EmailAddress]
        //public string Email { get; set; }

        [Required]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        [Required]
        [Display(Name = "Mật khẩu")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Ghi nhớ ?")]
        public bool RememberMe { get; set; }

        //public string IpAddress { get; set; }
    }
}
