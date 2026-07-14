using Google.Apis.Auth;
using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;
using Koralytics.Application.Interfaces.Auth;
using Microsoft.Extensions.Configuration;

namespace Koralytics.Infrastructure.ExternalServices
{
    public class GoogleOAuthProvider : IOAuthProvider
    {
        private readonly string _clientId;

        public GoogleOAuthProvider(IConfiguration configuration)
        {
            _clientId = configuration["OAuth:Google:ClientId"] 
                ?? throw new InvalidOperationException("Google ClientId is missing.");
        }

        public string ProviderName => "Google";

        public async Task<OAuthUserInfo> GetUserInfoAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _clientId }
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);

                return new OAuthUserInfo(
                    ProviderId: payload.Subject,
                    Email: payload.Email,
                    FirstName: payload.GivenName,
                    LastName: payload.FamilyName,
                    ProfileImageUrl: payload.Picture
                );
            }
            catch (InvalidJwtException ex)
            {
                throw new Exception("Invalid Google token.", ex);
            }
        }
    }
}
