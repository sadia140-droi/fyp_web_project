using System;
using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Models
{
    public class OrderReview
    {
        public int Id { get; set; }

        [Required]
        public string OrderId { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int KitchenId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(1000)]
        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Order Order { get; set; } = null!;

        public CustomerModel Customer { get; set; } = null!;

        public Kitchen Kitchen { get; set; } = null!;
    }
}