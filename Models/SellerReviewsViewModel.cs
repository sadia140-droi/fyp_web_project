using System;
using System.Collections.Generic;

namespace Cloud_Kitchen_web_App.Models
{
    public class SellerReviewsViewModel
    {
        public int KitchenId { get; set; }

        public string KitchenName { get; set; } = string.Empty;

        public double AverageRating { get; set; }

        public int TotalReviews { get; set; }

        public int Recent30DaysReviewsCount { get; set; }

        public int FiveStarCount { get; set; }

        public int FourStarCount { get; set; }

        public int ThreeStarCount { get; set; }

        public int TwoStarCount { get; set; }

        public int OneStarCount { get; set; }

        public List<SellerReviewItemViewModel> Reviews { get; set; } = new();
    }

    public class SellerReviewItemViewModel
    {
        public int Id { get; set; }

        public string OrderId { get; set; } = string.Empty;

        public string CustomerFirstName { get; set; } = string.Empty;

        public int Rating { get; set; }

        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }
}
