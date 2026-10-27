using System.Text.Json.Serialization;

namespace Cloud_Kitchen_web_App.Api.DTOs.Responses
{
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }
        public string? City { get; set; }
        public string Role { get; set; } = string.Empty;
        public string? SellerStatus { get; set; }
        public string? GoogleId { get; set; }
        public string? Picture { get; set; }
        public int? KitchenId { get; set; }
        public int? CustomerId { get; set; }
    }
}
