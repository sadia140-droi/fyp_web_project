using System.Collections.Generic;

namespace Cloud_Kitchen_web_App.Models
{
    public class OrdersViewModel
    {
        public string SellerName { get; set; }
        public string KitchenName { get; set; }
        public string StoreStatus { get; set; } // Open / Closed
        public List<Order> Orders { get; set; } = new();
    }
}
