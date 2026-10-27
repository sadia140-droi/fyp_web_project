using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Models
{
    public class MenuItem
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        [Range(0, 999999999.99)]
        public decimal Price { get; set; }

        public bool Available { get; set; } = true;

        // true = Today's Deal
        // false = Normal Menu Item
        public bool IsDeal { get; set; } = false;

        [MaxLength(500)]
        public string Image { get; set; } = string.Empty;

        public int KitchenId { get; set; }

        public Kitchen Kitchen { get; set; } = null!;
    }
}