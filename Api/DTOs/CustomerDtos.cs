using System;
using System.Collections.Generic;

namespace Cloud_Kitchen_web_App.Api.DTOs
{
    public class KitchenDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string DeliveryTime { get; set; } = string.Empty;
        public decimal MinOrder { get; set; }
        public decimal DeliveryRatePerKm { get; set; }
        public decimal DistanceKm { get; set; }
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Cuisine { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public bool IsOpen { get; set; }
        public string OpeningTime { get; set; } = string.Empty;
        public string ClosingTime { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public int ReviewCount { get; set; }
        public List<string> Categories { get; set; } = new();
    }

    public class KitchenDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Cuisine { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Area { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public string Image { get; set; } = string.Empty;
        public string OpeningTime { get; set; } = string.Empty;
        public string ClosingTime { get; set; } = string.Empty;
        public decimal DeliveryRatePerKm { get; set; }
        public string DeliveryAreas { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string DeliveryTime { get; set; } = string.Empty;
        public decimal MinOrder { get; set; }
        public bool IsOpen { get; set; }
        public decimal DistanceKm { get; set; }
        public bool IsFavorite { get; set; }
        public int ReviewCount { get; set; }
        public List<string> Categories { get; set; } = new();
        public List<CustomerReviewDto> Reviews { get; set; } = new();
        public List<MenuItemDto> MenuItems { get; set; } = new();
    }

    public class CustomerReviewDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PublicKitchenReviewDto
    {
        public int Id { get; set; }
        public string CustomerFirstName { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }

    public class PublicKitchenReviewsSummaryDto
    {
        public double AverageRating { get; set; }
        public int TotalReviews { get; set; }
        public int FiveStarCount { get; set; }
        public int FourStarCount { get; set; }
        public int ThreeStarCount { get; set; }
        public int TwoStarCount { get; set; }
        public int OneStarCount { get; set; }
    }

    public class PublicKitchenReviewsResponseDto
    {
        public int KitchenId { get; set; }
        public string KitchenName { get; set; } = string.Empty;
        public PublicKitchenReviewsSummaryDto Summary { get; set; } = new();
        public List<PublicKitchenReviewDto> Reviews { get; set; } = new();
    }

    public class MenuItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsDeal { get; set; }
        public string Image { get; set; } = string.Empty;
    }

    public class OrderRequestDto
    {
        public int KitchenId { get; set; }
        public string ReceiverName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string? Landmark { get; set; }
        public string? RiderInstruction { get; set; }
        public double CustomerLatitude { get; set; }
        public double CustomerLongitude { get; set; }
        public decimal? DeliveryFee { get; set; }
        public decimal? DistanceKm { get; set; }
        public List<CartItemDto> Items { get; set; } = new();
    }

    public class CartItemDto
    {
        public int MenuItemId { get; set; }
        public int Qty { get; set; }
    }

    public class OrderResponseDto
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal Total { get; set; }
    }

    public class CustomerProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string ProfilePictureUrl { get; set; } = string.Empty;
    }

    public class UpdateCustomerProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? ProfilePictureBase64 { get; set; }
    }

    public class ChangePasswordRequestDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class CustomerSettingsDto
    {
        public bool OrderNotifications { get; set; } = true;
        public bool PromotionalNotifications { get; set; } = true;
        public bool EmailNotifications { get; set; } = true;
        public bool ShowProfile { get; set; } = true;
        public string Language { get; set; } = "English";
    }

    public class UpdateCustomerSettingsRequest
    {
        public bool OrderNotifications { get; set; }
        public bool PromotionalNotifications { get; set; }
        public bool EmailNotifications { get; set; }
        public bool ShowProfile { get; set; }
        public string Language { get; set; } = "English";
    }

    public class DeliveryCalculationRequestDto
    {
        public int KitchenId { get; set; }
        public double CustomerLatitude { get; set; }
        public double CustomerLongitude { get; set; }
        public decimal? DeliveryFee { get; set; }
        public decimal? DistanceKm { get; set; }
    }

    public class DeliveryCalculationResponseDto
    {
        public int KitchenId { get; set; }
        public string KitchenName { get; set; } = string.Empty;
        public decimal DistanceKm { get; set; }
        public decimal DeliveryFee { get; set; }
        public decimal MinOrder { get; set; }
        public bool CanDeliver { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class FavoriteKitchenDto
    {
        public int FavoriteId { get; set; }
        public int KitchenId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Image { get; set; } = string.Empty;
        public double Rating { get; set; }
        public string Cuisine { get; set; } = string.Empty;
        public string DeliveryTime { get; set; } = string.Empty;
        public decimal MinOrder { get; set; }
        public bool IsOpen { get; set; }
        public decimal DistanceKm { get; set; }
    }
}
