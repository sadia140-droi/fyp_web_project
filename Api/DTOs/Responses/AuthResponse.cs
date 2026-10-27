using System.Text.Json.Serialization;

namespace Cloud_Kitchen_web_App.Api.DTOs.Responses
{
    public class AuthResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

        public string? Role { get; set; }
        public string? SellerStatus { get; set; }
        public UserDto? User { get; set; }
    }
}
