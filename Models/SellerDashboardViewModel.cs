using System.Collections.Generic;

namespace Cloud_Kitchen_web_App.Models
{
    public class SellerDashboardViewModel
    {
        // ============================================================
        // SELLER / KITCHEN INFORMATION
        // ============================================================

        public string SellerName { get; set; } = string.Empty;

        public string KitchenName { get; set; } = string.Empty;

        public string StoreStatus { get; set; } = string.Empty;


        // ============================================================
        // DASHBOARD STATISTICS
        // ============================================================

        public int TotalOrders { get; set; }

        public int OrdersThisWeek { get; set; }

        public int ActiveOrders { get; set; }

        public decimal TodayEarnings { get; set; }

        public double Rating { get; set; }

        public int TotalReviews { get; set; }


        // ============================================================
        // RECENT ORDERS
        // ============================================================

        public List<Order> RecentOrders { get; set; } = new();
    }
}