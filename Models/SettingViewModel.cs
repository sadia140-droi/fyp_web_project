using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Models
{
    public class SettingViewModel
    {
        // =========================================================
        // CUSTOMER SETTINGS
        // =========================================================

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
        // SELLER SETTINGS
        // =========================================================

        [StringLength(200)]
        [Display(Name = "Seller Name")]
        public string SellerName { get; set; } = string.Empty;

        [StringLength(200)]
        [Display(Name = "Kitchen Name")]
        public string KitchenName { get; set; } = string.Empty;

        [Display(Name = "Notifications Enabled")]
        public bool NotificationsEnabled { get; set; } = true;

        [Display(Name = "Store Open")]
        public bool StoreOpen { get; set; } = true;

        [Display(Name = "Open Time")]
        public string OpenTime { get; set; } = "09:00";

        [Display(Name = "Close Time")]
        public string CloseTime { get; set; } = "22:00";

        [Display(Name = "Delivery Rate per KM (PKR)")]
        public decimal DeliveryRatePerKm { get; set; } = 30m;

        public string City { get; set; } = string.Empty;

        public string Area { get; set; } = string.Empty;

        public double Lat { get; set; }

        public double Lng { get; set; }
    }
}