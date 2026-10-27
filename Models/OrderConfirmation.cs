namespace Cloud_Kitchen_web_App.Models
{
    public class OrderConfirmationViewModel
    {
        public int OrderId { get; set; }
        public string KitchenName { get; set; } = "Kitchen";
        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal DeliveryDistanceKm { get; set; }
        public decimal DeliveryRatePerKm { get; set; }
        public decimal TotalAmount { get; set; }
    }
}