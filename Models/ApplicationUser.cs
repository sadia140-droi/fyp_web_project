using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Models
{
    public class ApplicationUser : IdentityUser
    {
        // ============================================================
        // USER BASIC INFORMATION
        // ============================================================

        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;


        [MaxLength(100)]
        public string City { get; set; } = string.Empty;


        // ============================================================
        // USER ROLE
        // Customer / Seller / Admin
        //
        // NOTE:
        // ASP.NET Identity Roles are ALSO stored separately.
        // We will verify that both stay synchronized.
        // ============================================================

        [Required]
        [MaxLength(20)]
        public string UserRole { get; set; } = "Customer";


        // ============================================================
        // SELLER APPROVAL STATUS
        // Pending / Approved / Rejected
        //
        // Customer/Admin don't normally need approval.
        // ============================================================

        [Required]
        [MaxLength(20)]
        public string SellerStatus { get; set; } = "Pending";

        // ============================================================
        // JWT REFRESH TOKEN
        // ============================================================

        [MaxLength(500)]
        public string? RefreshTokenHash { get; set; }

        public DateTime? RefreshTokenExpiryTime { get; set; }
    }
}