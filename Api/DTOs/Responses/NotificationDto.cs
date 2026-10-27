using System;

namespace Cloud_Kitchen_web_App.Api.DTOs.Responses
{
    public class NotificationDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public DateTime Time { get; set; }
        public bool IsRead { get; set; }
        public string RelatedOrderId { get; set; }
    }
}
