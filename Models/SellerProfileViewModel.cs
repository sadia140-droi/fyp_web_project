namespace Cloud_Kitchen_web_App.Models
{
    public class SellerProfileViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Shop { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal DeliveryRatePerKm { get; set; } = 30m;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Photo { get; set; } = string.Empty;
    }
}
