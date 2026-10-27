using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cloud_Kitchen_web_App.Api.Services
{
    public class GoogleAuthService : IGoogleAuthService
    {
        public const string PrimaryClientId = "159284505884-gmrgti0a055v1ub3c6imc3sgg8382frj.apps.googleusercontent.com";
        public const string SecondaryClientId = "159284505884-f9krc67aes4j0ojj566o9754p2gd8ne8.apps.googleusercontent.com";
        private readonly IConfiguration _configuration;
        private readonly ILogger<GoogleAuthService> _logger;

        public GoogleAuthService(IConfiguration configuration, ILogger<GoogleAuthService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<GoogleUserInfo> VerifyGoogleTokenAsync(string idToken)
        {
            if (string.IsNullOrWhiteSpace(idToken))
            {
                throw new ArgumentException("ID token cannot be null or empty.", nameof(idToken));
            }

            var configuredClientId = _configuration["Authentication:Google:ClientId"]
                                  ?? _configuration["Google:ClientId"]
                                  ?? _configuration["GoogleAuth:ClientId"];
            var androidClientId = _configuration["Authentication:Google:AndroidClientId"]
                               ?? _configuration["Google:AndroidClientId"];

            var targetClientId = !string.IsNullOrWhiteSpace(configuredClientId)
                ? configuredClientId.Trim()
                : $"{PrimaryClientId},{SecondaryClientId}";

            var clientIds = targetClientId.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var audienceList = new HashSet<string>(clientIds, StringComparer.OrdinalIgnoreCase)
            {
                PrimaryClientId,
                SecondaryClientId
            };
            if (!string.IsNullOrWhiteSpace(androidClientId))
            {
                audienceList.Add(androidClientId.Trim());
            }

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = audienceList
            };

            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                var fullName = !string.IsNullOrWhiteSpace(payload.Name)
                    ? payload.Name
                    : (!string.IsNullOrWhiteSpace(payload.GivenName)
                        ? $"{payload.GivenName} {payload.FamilyName}".Trim()
                        : (payload.Email?.Split('@')[0] ?? string.Empty));

                return new GoogleUserInfo
                {
                    GoogleId = payload.Subject, // Google user ID (sub)
                    Email = payload.Email ?? string.Empty,
                    Name = fullName,
                    Picture = payload.Picture,
                    EmailVerified = payload.EmailVerified
                };
            }
            catch (InvalidJwtException ex)
            {
                _logger.LogWarning(ex, "Failed to validate Google ID token: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during Google token verification.");
                throw;
            }
        }
    }
}
