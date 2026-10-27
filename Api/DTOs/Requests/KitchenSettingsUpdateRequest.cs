namespace Cloud_Kitchen_web_App.Api.DTOs.Requests
{
    public class KitchenSettingsUpdateRequest
    {
        public bool IsStoreOpen { get; set; }
        public string OpenTime { get; set; } = string.Empty;
        public string CloseTime { get; set; } = string.Empty;
    }
}
