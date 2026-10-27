using System.Collections.Generic;

namespace Cloud_Kitchen_web_App.Models
{
    public class KitchenPageViewModel
    {
        public List<KitchenViewModel> Kitchens { get; set; } = new();

        public List<DealViewModel> Deals { get; set; } = new();

        public List<CategoryViewModel> Categories { get; set; } = new();

        public List<LocationViewModel> Locations { get; set; } = new();
    }

    public class CategoryViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
    }

    public class KitchenViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Image { get; set; } = string.Empty;

        public double Rating { get; set; }

        public string Time { get; set; } = string.Empty;

        public double Lat { get; set; }

        public double Lng { get; set; }

        public string City { get; set; } = string.Empty;

        public string Area { get; set; } = string.Empty;

        public double? DistanceKm { get; set; }

        public bool IsFavorite { get; set; }

        public List<string> Tags { get; set; } = new();

        public string DefaultFood { get; set; } = string.Empty;

        public decimal DefaultPrice { get; set; }

        public bool IsOpen { get; set; } = true;

        public string OpeningTime { get; set; } = string.Empty;

        public string ClosingTime { get; set; } = string.Empty;

        public decimal DeliveryRatePerKm { get; set; } = 30m;

        public decimal MinOrder { get; set; } = 100m;
    }

    public class LocationViewModel
    {
        public string City { get; set; } = string.Empty;

        public string Area { get; set; } = string.Empty;

        public double Lat { get; set; }

        public double Lng { get; set; }
    }

    public class DealViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string Image { get; set; } = string.Empty;

        public int KitchenId { get; set; }

        public string KitchenName { get; set; } = string.Empty;
    }
}