using System.Collections.Generic;

namespace Cloud_Kitchen_web_App.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }

        public int TotalKitchens { get; set; }

        public int TotalOrders { get; set; }

        public decimal Revenue { get; set; }

        public List<RecentOrderViewModel> RecentOrders { get; set; }
            = new List<RecentOrderViewModel>();

        // Chart Data Properties
        public List<string> Last7DaysLabels { get; set; } = new List<string>();
        public List<decimal> Last7DaysRevenue { get; set; } = new List<decimal>();
        public List<string> TopKitchenNames { get; set; } = new List<string>();
        public List<int> TopKitchenOrderCounts { get; set; } = new List<int>();
    }


    public class RecentOrderViewModel
    {
        public string OrderId { get; set; } = string.Empty;

        public string Customer { get; set; } = string.Empty;

        public string Kitchen { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public decimal Amount { get; set; }
    }
}
