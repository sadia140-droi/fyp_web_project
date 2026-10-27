namespace Cloud_Kitchen_web_App.Api.DTOs.Responses
{
    public class MenuItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool Available { get; set; }
        public bool IsDeal { get; set; }
        public string Image { get; set; } = string.Empty;
        public int KitchenId { get; set; }
    }
}
