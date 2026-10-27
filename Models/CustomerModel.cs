using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Models
{
    public class CustomerModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty;

        [Phone]
        [StringLength(50)]
        public string Phone { get; set; } = string.Empty;

        [StringLength(500)]
        public string Address { get; set; } = string.Empty;

        // Customer's latest saved delivery location.
        // Actual order location will still be stored separately
        // inside Order as a snapshot.
        [Range(-90, 90)]
        public double? Latitude { get; set; }

        [Range(-180, 180)]
        public double? Longitude { get; set; }

        [StringLength(500)]
        public string ProfilePictureUrl { get; set; } = "/images/logo.png";
    }
}