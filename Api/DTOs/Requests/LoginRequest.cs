using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Api.DTOs.Requests
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }
}
