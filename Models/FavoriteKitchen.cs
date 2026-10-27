using System;
using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Models
{
    public class FavoriteKitchen
    {
        public int Id { get; set; }

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int KitchenId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public CustomerModel Customer { get; set; } = null!;

        public Kitchen Kitchen { get; set; } = null!;
    }
}