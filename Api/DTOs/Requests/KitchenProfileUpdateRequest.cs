namespace Cloud_Kitchen_web_App.Api.DTOs.Requests
{
    public class KitchenProfileUpdateRequest
    {
        public string KitchenName { get; set; } = string.Empty;
        public string KitchenPhone { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Cuisine { get; set; } = string.Empty;
    }
}
