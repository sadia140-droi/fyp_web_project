using System;

namespace Cloud_Kitchen_web_App.Api.DTOs.Requests
{
    public class ReviewRequestDto
    {
        public int Rating { get; set; }
        public string Comment { get; set; }
    }
}
