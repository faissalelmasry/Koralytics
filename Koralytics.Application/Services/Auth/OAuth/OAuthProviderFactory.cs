using System;
using System.Collections.Generic;
using System.Linq;
using Koralytics.Application.Interfaces.Auth;

namespace Koralytics.Application.Services.Auth.OAuth
{
    public class OAuthProviderFactory : IOAuthProviderFactory
    {
        private readonly IEnumerable<IOAuthProvider> _providers;

        public OAuthProviderFactory(IEnumerable<IOAuthProvider> providers)
        {
            _providers = providers;
        }

        public IOAuthProvider GetProvider(string providerName)
        {
            var provider = _providers.FirstOrDefault(p => p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
            
            if (provider == null)
            {
                throw new NotSupportedException($"OAuth provider '{providerName}' is not supported.");
            }
            
            return provider;
        }
    }
}
