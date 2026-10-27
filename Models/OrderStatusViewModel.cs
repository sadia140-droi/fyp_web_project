namespace Cloud_Kitchen_web_App.Models
{
    public class OrderStatusViewModel
    {
        public int OrderId { get; set; }

        public string KitchenName { get; set; } = "Kitchen Owner";

        public decimal Subtotal { get; set; }

        public decimal DeliveryFee { get; set; }

        public decimal DeliveryDistanceKm { get; set; }

        public decimal DeliveryRatePerKm { get; set; }

        public decimal TotalAmount { get; set; }

        public string ExpectedTime { get; set; } = "30-45 Mins";

        // Current order status
        public string Status { get; set; } = "Order Placed";

        // Used to control Cancel Order button
        public bool CanCancel { get; set; }

        // Optional message shown after cancellation
        public string CancellationMessage { get; set; } = string.Empty;

        // The deadline time for cancellation, if applicable
        public DateTime? CancellationDeadline { get; set; }
    }
}