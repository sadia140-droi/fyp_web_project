using System.Collections.Generic;

namespace Cloud_Kitchen_web_App.Models
{
    public class AdminSettingsViewModel
    {
        public List<string> Categories { get; set; } = new List<string>
        {
            "Pizza",
            "Biryani",
            "Desserts"
        };

        public List<OfferViewModel> Offers { get; set; } = new List<OfferViewModel>
        {
            new OfferViewModel
            {
                Title = "Weekend Special",
                Discount = 15
            },

            new OfferViewModel
            {
                Title = "Holiday Sale",
                Discount = 20
            }
        };

        public bool EnableNotifications { get; set; } = true;

        public bool EnableCashOnDelivery { get; set; } = false;

        public bool ShowPopularItems { get; set; } = true;
    }

    public class OfferViewModel
    {
        public string Title { get; set; } = string.Empty;

        public int Discount { get; set; }
    }
}
