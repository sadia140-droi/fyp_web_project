using System;
using System.Collections.Generic;

namespace Cloud_Kitchen_web_App.Api.DTOs.Responses
{
    public class OrderDto
    {
        public string Id { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string Customer { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double? CustomerLatitude { get; set; }
        public double? CustomerLongitude { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal Total { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public DateTime PlacedAt { get; set; }
        public DateTime? AcceptedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int KitchenId { get; set; }
        
        public List<OrderItemDto> Items { get; set; } = new();
    }
}
