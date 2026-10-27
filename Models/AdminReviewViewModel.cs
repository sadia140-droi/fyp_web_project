using System;

namespace Cloud_Kitchen_web_App.Models
{
    public class AdminReviewViewModel
    {
        public int Id { get; set; }

        public string OrderId { get; set; } = string.Empty;

        public int KitchenId { get; set; }

        public string KitchenName { get; set; } = string.Empty;

        public int CustomerId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public int Rating { get; set; }

        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
