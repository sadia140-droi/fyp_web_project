using System;
using System.Collections.Generic;
using Cloud_Kitchen_web_App.Api.DTOs.Responses;

namespace Cloud_Kitchen_web_App.Api.DTOs
{
    public class SellerDashboardDto
    {
        public int TodayOrdersCount { get; set; }
        public decimal TodayRevenue { get; set; }
        public int TotalOrdersCount { get; set; }
        public decimal TotalRevenue { get; set; }
        public int PendingOrdersCount { get; set; }
        public int CompletedOrdersCount { get; set; }
        public double Rating { get; set; }
        public int TotalReviews { get; set; }
        public List<OrderDto> RecentOrders { get; set; } = new();
        public List<CustomerReviewDto> RecentReviews { get; set; } = new();
    }

    public class SellerOwnerProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string KitchenName { get; set; } = string.Empty;
        public string ApprovalStatus { get; set; } = string.Empty;
    }

    public class UpdateSellerOwnerProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class CancelSellerOrderRequest
    {
        public string Reason { get; set; } = string.Empty;
    }

    public class SellerReviewsSummaryDto
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int Recent30DaysCount { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }
    }

    public class SellerReviewsResponseDto
    {
        public int KitchenId { get; set; }
        public string KitchenName { get; set; } = string.Empty;
        public SellerReviewsSummaryDto Summary { get; set; } = new();
        public List<CustomerReviewDto> Reviews { get; set; } = new();
    }
}
