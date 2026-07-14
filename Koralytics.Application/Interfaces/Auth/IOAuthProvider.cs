using System.Threading.Tasks;
using Koralytics.Application.DTOs.AuthDTOs.LoginDTOs;

namespace Koralytics.Application.Interfaces.Auth
{
    public interface IOAuthProvider
    {
        string ProviderName { get; }
        Task<OAuthUserInfo> GetUserInfoAsync(string idToken);
    }
}
