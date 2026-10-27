using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Cloud_Kitchen_web_App.Models
{
    public class MenuItemInputViewModel
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Range(0, 999999999.99)]
        public decimal Price { get; set; }

        public bool Available { get; set; } = true;

        public bool IsDeal { get; set; }

        public IFormFile? Image { get; set; }
    }
}