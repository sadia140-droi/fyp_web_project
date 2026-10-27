using System;
using System.Collections.Generic;

namespace Cloud_Kitchen_web_App.Models
{
    public class OrderHistoryViewModel
    {
        public List<OrderHistoryItem> PastOrders { get; set; } = new List<OrderHistoryItem>();
    }

    public class OrderHistoryItem
    {
        public int Id { get; set; }
        public string KitchenName { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Preparing, Completed
        public string ItemsSummary { get; set; } = string.Empty; // e.g., "Chicken Biryani x1, Thandi Kheer x1"
        public int? Rating { get; set; }
        public string? Comment { get; set; }
    }
}