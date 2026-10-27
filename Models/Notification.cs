using System;

namespace Cloud_Kitchen_web_App.Models
{
    public class Notification
    {
        public string Id { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;

        public DateTime Time { get; set; } = DateTime.UtcNow;

        public string Status { get; set; } = "Unread";

        public string Link { get; set; } = string.Empty;

        // Customer = CustomerModel
        // Seller   = KitchenOwner
        public string RecipientType { get; set; } = string.Empty;

        // CustomerModel.Id OR KitchenOwner.Id
        public int RecipientId { get; set; }

        // Filled when notification is related to an order
        public string? OrderId { get; set; }
    }
}