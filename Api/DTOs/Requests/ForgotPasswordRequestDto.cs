using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Api.DTOs.Requests
{
    public class ForgotPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }
    }
}
