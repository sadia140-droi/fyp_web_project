using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Cloud_Kitchen_web_App.Api.DTOs.Requests
{
    public class GoogleSignInRequest
    {
        [JsonPropertyName("idToken")]
        public string? IdToken { get; set; }
    }
}
