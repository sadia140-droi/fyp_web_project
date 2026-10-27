using System.Threading.Tasks;

namespace Cloud_Kitchen_web_App.Api.Services
{
    public class GoogleUserInfo
    {
        public string GoogleId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Picture { get; set; }
        public bool EmailVerified { get; set; }
    }

    public interface IGoogleAuthService
    {
        Task<GoogleUserInfo> VerifyGoogleTokenAsync(string idToken);
    }
}
