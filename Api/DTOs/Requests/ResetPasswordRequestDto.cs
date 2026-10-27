using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Api.DTOs.Requests
{
    public class ResetPasswordRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Token { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }
    }
}
