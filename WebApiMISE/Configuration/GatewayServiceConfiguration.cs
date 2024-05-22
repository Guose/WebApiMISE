using WebApiMISE.Exceptions;
using WebApiMISE.Interfaces;

namespace WebApiMISE.Configuration
{
    public class GatewayServiceConfiguration : BaseServiceConfiguration, IConfigPropagation
    {
        /// <summary>
        /// Gets or sets Kestrel Configuration override files
        /// </summary>
        public IEnumerable<string> KestrelConfigurationOverrides { get; set; } = Array.Empty<string>();

        public string? Environment { get; set; }

        public KeyVaultManagerConfiguration KeyVaultManagerConfiguration { get; set; } = new KeyVaultManagerConfiguration();

        public AuthenticationConfiguration AuthenticationConfiguration { get; set; } = new AuthenticationConfiguration();

        public IdentityConfiguration? IdentityConfiguration { get; set; }

        
        public void PropagateConfig()
        {
            // if identity config is set, try to propagate it to all other unset identity config
            if (this.IdentityConfiguration != default)
            {
                foreach (var config in this.GetPropertiesOfType<IAuthenticationConfiguration>())
                {
                    if (config!.IdentityConfiguration == default)
                    {
                        config.IdentityConfiguration = this.IdentityConfiguration;
                    }
                }
            }
        }

        /// <summary>
        /// Validates the configuration
        /// </summary>
        public override void Validate()
        {
            if (this.MaxConcurrentConnections <= 0)
            {
                throw new ArgumentOutOfRangeException("Value must be > 0", nameof(this.MaxConcurrentConnections));
            }

            return;
        }
    }
}
