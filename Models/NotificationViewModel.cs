using System.Collections.Generic;

namespace Cloud_Kitchen_web_App.Models
{
    public class NotificationViewModel
    {
        public List<Notification> Notifications { get; set; }
            = new List<Notification>();
    }
}