using Cloud_Kitchen_web_App.Models;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cloud_Kitchen_web_App.Api.Services
{
    public interface IJwtService
    {
        Task<string> GenerateAccessTokenAsync(ApplicationUser user);
        string GenerateRefreshToken();
        string HashRefreshToken(string refreshToken);
        ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    }
}
