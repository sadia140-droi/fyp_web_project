using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Models
{
    public class AdminForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email address is required")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address")]
        public string Email { get; set; } = "";
    }
}
