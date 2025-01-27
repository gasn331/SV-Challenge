using SmartVault.Program.BusinessObjects;
using System;

namespace SmartVault.Program.Services
{
    public class OAuthManager
    {
        public OAuthIntegration OAuthIntegration { get; set; }

        public OAuthManager(OAuthIntegration oauthIntegration)
        {
            OAuthIntegration = oauthIntegration;
        }

        public bool IsTokenExpired()
        {
            return OAuthIntegration.TokenExpiration <= DateTime.UtcNow;
        }

        public void RefreshToken()
        {
            OAuthIntegration.AccessToken = "newAccessToken"; // Simulate new token
            OAuthIntegration.RefreshToken = "newRefreshToken"; // Simulate new refresh token
            OAuthIntegration.TokenExpiration = DateTime.UtcNow.AddHours(1); // New expiration time
        }

        public void StoreOAuthData()
        {
            // Mock saving OAuth data
            Console.WriteLine($"Saving OAuth Data: {OAuthIntegration.AccessToken}, {OAuthIntegration.RefreshToken}, {OAuthIntegration.TokenExpiration}");
        }
    }
}
