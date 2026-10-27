using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cloud_Kitchen_web_App.Models
{
    public class Order
    {
        // ============================================================
        // ORDER ID
        // ============================================================

        [Required]
        [MaxLength(50)]
        public string Id { get; set; } = string.Empty;


        // ============================================================
        // CUSTOMER
        // ============================================================

        // Real database relationship.
        // Every order belongs to one registered customer.
        [Required]
        public int CustomerId { get; set; }

        public CustomerModel CustomerAccount { get; set; } = null!;


        // Customer information snapshot at the time of order.
        // These values remain unchanged even if the customer's profile
        // is updated later.
        [Required]
        [MaxLength(200)]
        public string Customer { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Address { get; set; } = string.Empty;

        public double? CustomerLatitude { get; set; }

        public double? CustomerLongitude { get; set; }


        // ============================================================
        // ORDER ITEMS
        // ============================================================

        // Owned collection.
        // Prices/names are snapshots from the time the order was placed.
        public List<OrderItem> Items { get; set; } = new();


        // ============================================================
        // ORDER AMOUNTS
        // ============================================================

        public decimal Subtotal { get; set; }

        public decimal DeliveryFee { get; set; }

        public decimal DeliveryDistanceKm { get; set; }

        public decimal Total { get; set; }


        // ============================================================
        // PAYMENT
        // ============================================================

        [Required]
        [MaxLength(50)]
        public string PaymentMethod { get; set; } = "COD";


        // ============================================================
        // ORDER TIMING
        // ============================================================

        public DateTime PlacedAt { get; set; } = DateTime.UtcNow;

        // Null = seller has not accepted the order yet.
        // Value = exact UTC time when seller accepted the order.
        //
        // Customer cancellation window is calculated from this value.
        // Allowed cancellation period: 5 minutes.
        public DateTime? AcceptedAt { get; set; }


        // ============================================================
        // ORDER STATUS
        // ============================================================

        // Initial:
        // Order Placed
        //
        // Seller:
        // Accepted
        //
        // Later:
        // Preparing
        // Out for Delivery
        // Delivered
        //
        // Customer:
        // Cancelled
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Order Placed";


        // ============================================================
        // KITCHEN
        // ============================================================

        [Required]
        public int KitchenId { get; set; }

        public Kitchen Kitchen { get; set; } = null!;
    }
}