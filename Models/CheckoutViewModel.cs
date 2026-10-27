using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Models
{
    public class CheckoutViewModel
    {
        // =========================================================
        // KITCHEN
        // =========================================================

        [Required]
        public int KitchenId { get; set; }

        [StringLength(200)]
        public string? KitchenName { get; set; } = string.Empty;

        public decimal DeliveryRatePerKm { get; set; } = 30m;

        public double KitchenLat { get; set; }

        public double KitchenLng { get; set; }

        [StringLength(100)]
        public string KitchenCity { get; set; } = string.Empty;

        [StringLength(200)]
        public string KitchenArea { get; set; } = string.Empty;


        // =========================================================
        // CUSTOMER DELIVERY INFORMATION
        // =========================================================

        [Required]
        [StringLength(200)]
        public string ReceiverName { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(50)]
        public string PhoneNumber { get; set; } = string.Empty;

        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(200)]
        public string Area { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string DeliveryAddress { get; set; } = string.Empty;

        [StringLength(200)]
        public string Landmark { get; set; } = string.Empty;

        [StringLength(500)]
        public string RiderInstruction { get; set; } = string.Empty;


        // =========================================================
        // CUSTOMER DELIVERY LOCATION
        // =========================================================

        [Range(-90, 90)]
        public double CustomerLatitude { get; set; }

        [Range(-180, 180)]
        public double CustomerLongitude { get; set; }


        // =========================================================
        // PAYMENT
        // =========================================================

        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "COD";


        // =========================================================
        // CART
        // =========================================================

        // Temporary cart payload from browser.
        // Server MUST validate all items against the database.
        [Required]
        public string CartJson { get; set; } = string.Empty;


        // =========================================================
        // CALCULATED VALUES
        // =========================================================

        public decimal Subtotal { get; set; }

        public decimal DeliveryDistanceKm { get; set; }

        public decimal DeliveryFee { get; set; }

        // Final total validated from database data during PlaceOrder()
        public decimal TotalAmount { get; set; }
    }
}