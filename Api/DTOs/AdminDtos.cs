using System;
using System.Collections.Generic;

namespace Cloud_Kitchen_web_App.Api.DTOs
{
    public class AdminDashboardDto
    {
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int TotalKitchens { get; set; }
        public int PendingKitchens { get; set; }
        public int ApprovedKitchens { get; set; }
        public int RejectedKitchens { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalSellers { get; set; }
        public List<AdminOrderSummaryDto> RecentOrders { get; set; } = new();
    }

    public class AdminKitchenDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OwnerName { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Cuisine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public bool IsOpen { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public string RejectionReason { get; set; } = string.Empty;
        public double Rating { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSales { get; set; }
    }

    public class AdminUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public bool IsLockedOut { get; set; }
        public string SellerStatus { get; set; } = string.Empty;
    }

    public class EditUserRequestDto
    {
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
    }

    public class AdminCategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string ImagePath { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int ItemCount { get; set; }
    }

    public class CategoryRequestDto
    {
        public string Name { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class RejectKitchenRequestDto
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class AdminOrderSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Customer { get; set; } = string.Empty;
        public string KitchenName { get; set; } = string.Empty;
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime PlacedAt { get; set; }
    }
}
