using System.Collections.Generic;

namespace Cloud_Kitchen_web_App.Models
{
    public class MenuViewModel
    {
        public string KitchenName { get; set; } = string.Empty;

        public List<MenuItem> Items { get; set; } = new();
    }
}