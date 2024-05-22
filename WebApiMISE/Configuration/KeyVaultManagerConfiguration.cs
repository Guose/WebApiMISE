using WebApiMISE.Interfaces;

namespace WebApiMISE.Configuration
{
    public class KeyVaultManagerConfiguration : IValidatable, IAuthenticationConfiguration
    {
        public string? KeyVaultUri { get; set; }

        public int CacheTimeoutInMinutes { get; set; } = 60;
        public IdentityConfiguration? IdentityConfiguration { get; set; }

        public void Validate()
        {
            ArgumentException.ThrowIfNullOrEmpty(KeyVaultUri, nameof(KeyVaultUri));
            IdentityConfiguration?.Validate();
        }
    }
}
