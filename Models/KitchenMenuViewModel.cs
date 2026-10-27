using System.Collections.Generic;

namespace Cloud_Kitchen_web_App.Models
{
    // ============================================================
    // KITCHEN MENU VIEW MODEL
    // ============================================================

    public class KitchenMenuViewModel
    {
        public int KitchenId { get; set; }

        public string KitchenName { get; set; } = string.Empty;

        public string Image { get; set; } = string.Empty;

        public double Rating { get; set; }

        public decimal DeliveryRatePerKm { get; set; } = 30m;

        public decimal MinOrder { get; set; } = 100m;

        public double Lat { get; set; }

        public double Lng { get; set; }

        public string DeliveryTime { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string Area { get; set; } = string.Empty;

        public int TotalReviews { get; set; }

        public int FiveStarCount { get; set; }

        public int FourStarCount { get; set; }

        public int ThreeStarCount { get; set; }

        public int TwoStarCount { get; set; }

        public int OneStarCount { get; set; }

        public List<KitchenReviewItemViewModel> Reviews { get; set; } = new();

        public List<MenuCategoryViewModel> Categories { get; set; } = new();
    }

    // ============================================================
    // KITCHEN REVIEW ITEM VIEW MODEL
    // ============================================================

    public class KitchenReviewItemViewModel
    {
        public int Rating { get; set; }

        public string Comment { get; set; } = string.Empty;

        public string CustomerFirstName { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }
    }


    // ============================================================
    // MENU CATEGORY VIEW MODEL
    // ============================================================

    public class MenuCategoryViewModel
    {
        public string CategoryName { get; set; } = string.Empty;

        public List<MenuItemViewModel> Items { get; set; } = new();
    }


    // ============================================================
    // MENU ITEM VIEW MODEL
    // ============================================================

    public class MenuItemViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public bool Available { get; set; }

        public bool IsDeal { get; set; }

        public string Image { get; set; } = string.Empty;

        public decimal MinOrder { get; set; }
    }
}