using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Models
{
    public class CustomerSettings
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Display(Name = "Order Notifications")]
        public bool OrderNotifications { get; set; } = true;

        [Display(Name = "Promotional Notifications")]
        public bool PromotionalNotifications { get; set; } = true;

        [Display(Name = "Email Notifications")]
        public bool EmailNotifications { get; set; } = true;

        [Display(Name = "Show Profile")]
        public bool ShowProfile { get; set; } = true;

        [Required]
        [StringLength(50)]
        [Display(Name = "Language")]
        public string Language { get; set; } = "English";

        // =========================================================
        // CUSTOMER RELATIONSHIP
        // =========================================================

        public CustomerModel Customer { get; set; } = null!;
    }
}