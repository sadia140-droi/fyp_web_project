using System.ComponentModel.DataAnnotations;

namespace CloudKitchen.Models
{
    public class FavoriteKitchenViewModel
    {
        [Required]
        public int KitchenId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string Area { get; set; } = string.Empty;

        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(100)]
        public string Tag { get; set; } = string.Empty;

        [StringLength(50)]
        public string Rating { get; set; } = string.Empty;
    }
}