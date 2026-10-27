namespace Cloud_Kitchen_web_App.Api.DTOs.Responses
{
    public class KitchenDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Cuisine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Image { get; set; } = string.Empty;
        public string OpeningTime { get; set; } = string.Empty;
        public string ClosingTime { get; set; } = string.Empty;
        public decimal DeliveryRatePerKm { get; set; }
        public string DeliveryAreas { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string DeliveryTime { get; set; } = string.Empty;
        public decimal MinOrder { get; set; }
        public bool IsOpen { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
    }
}
