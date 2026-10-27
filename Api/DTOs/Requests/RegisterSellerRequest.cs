using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Api.DTOs.Requests
{
    public class RegisterSellerRequest
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Google Email address (@gmail.com) is required.")]
        [RegularExpression(@"^[a-zA-Z0-9._%+-]+@(gmail\.com|googlemail\.com)$", ErrorMessage = "Only valid Google accounts (@gmail.com) are allowed.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [RegularExpression(@"^(\+92|92|0)?3[0-5][0-9]{8}$", ErrorMessage = "Please enter a valid Pakistani mobile number (e.g., 03001234567 or +923001234567).")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string KitchenName { get; set; } = string.Empty;

        [Required]
        public string KitchenPhone { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string Cuisine { get; set; } = string.Empty;

        [Required]
        public string City { get; set; } = string.Empty;

        [Required]
        public string Area { get; set; } = string.Empty;

        [Required]
        public string Address { get; set; } = string.Empty;

        public string OpeningTime { get; set; } = string.Empty;

        public string ClosingTime { get; set; } = string.Empty;

        public decimal DeliveryRatePerKm { get; set; }

        public string DeliveryAreas { get; set; } = string.Empty;

        public string ImageBase64 { get; set; } = string.Empty;
        
        public double Lat { get; set; }
        
        public double Lng { get; set; }
    }
}
