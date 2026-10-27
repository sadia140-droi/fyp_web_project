using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Api.DTOs.Requests
{
    public class RegisterCustomerRequest
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Google Email address (@gmail.com) is required.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|googlemail\.com)$", ErrorMessage = "Only valid Google accounts (@gmail.com) are allowed.")]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^(\+92|92|0)?3[0-5][0-9]{8}$", ErrorMessage = "Please enter a valid Pakistani mobile number (e.g., 03001234567 or +923001234567).")]
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;
    }
}
