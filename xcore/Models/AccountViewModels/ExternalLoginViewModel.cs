using System.ComponentModel.DataAnnotations;

namespace Models
{
    public class ExternalLoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
