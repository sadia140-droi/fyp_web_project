using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Cloud_Kitchen_web_App.Api.DTOs.Requests
{
    public class MenuItemRequest
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Range(0, 999999999.99)]
        public decimal Price { get; set; }

        public bool Available { get; set; } = true;

        public bool IsDeal { get; set; } = false;

        public string ImageBase64 { get; set; } = string.Empty;

        public IFormFile Image { get; set; }
    }
}
