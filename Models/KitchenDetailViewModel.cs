namespace Cloud_Kitchen_web_App.Models
{
    public class ItemDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Image { get; set; } = string.Empty;

        // Parent Kitchen details for navigation and cart context
        public int KitchenId { get; set; }
        public string KitchenName { get; set; } = string.Empty;
    }
}