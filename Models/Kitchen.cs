using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Models
{
    public class Kitchen
    {
        public int Id { get; set; }

        // =========================
        // BASIC KITCHEN INFORMATION
        // =========================

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Cuisine { get; set; } = string.Empty;

        // =========================
        // LOCATION
        // =========================

        [MaxLength(100)]
        public string City { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Area { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        public double Lat { get; set; }

        public double Lng { get; set; }

        // =========================
        // KITCHEN IMAGE
        // =========================

        [MaxLength(500)]
        public string Image { get; set; } = string.Empty;

        // =========================
        // OPENING / CLOSING TIME
        // =========================

        [MaxLength(20)]
        public string OpeningTime { get; set; } = string.Empty;

        [MaxLength(20)]
        public string ClosingTime { get; set; } = string.Empty;

        // =========================
        // DELIVERY INFORMATION
        // =========================

        public decimal DeliveryRatePerKm { get; set; }

        [MaxLength(1000)]
        public string DeliveryAreas { get; set; } = string.Empty;

        // =========================
        // EXISTING KITCHEN SETTINGS
        // =========================

        public double Rating { get; set; }

        [MaxLength(50)]
        public string DeliveryTime { get; set; } = string.Empty;

        public decimal MinOrder { get; set; }

        public bool IsOpen { get; set; } = true;

        // =========================
        // ADMIN APPROVAL
        // =========================

        [Required]
        [MaxLength(20)]
        public string ApprovalStatus { get; set; } = "Pending";

        [MaxLength(1000)]
        public string RejectionReason { get; set; } = string.Empty;

        // =========================
        // KITCHEN OWNER
        // =========================

        public int KitchenOwnerId { get; set; }

        public KitchenOwner KitchenOwner { get; set; } = null!;

        // =========================
        // MENU ITEMS
        // =========================

        public List<MenuItem> MenuItems { get; set; } = new();
    }
}