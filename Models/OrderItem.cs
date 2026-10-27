using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Models
{
    public class OrderItem
    {
        [Required]
        public int MenuItemId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Range(1, 100)]
        public int Qty { get; set; }

        public decimal Price { get; set; }
    }
}