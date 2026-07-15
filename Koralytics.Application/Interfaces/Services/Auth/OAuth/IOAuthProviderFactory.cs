using Koralytics.Application.Interfaces.Auth;

namespace Koralytics.Application.Services.Auth.OAuth
{
    public interface IOAuthProviderFactory
    {
        IOAuthProvider GetProvider(string providerName);
    }
}
