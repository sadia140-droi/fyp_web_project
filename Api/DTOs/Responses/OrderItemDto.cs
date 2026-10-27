namespace Cloud_Kitchen_web_App.Api.DTOs.Responses
{
    public class OrderItemDto
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Qty { get; set; }
        public decimal Price { get; set; }
    }
}
