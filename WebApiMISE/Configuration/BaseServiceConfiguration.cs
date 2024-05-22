using WebApiMISE.Interfaces;

namespace WebApiMISE.Configuration
{
    public abstract class BaseServiceConfiguration : IValidatable
    {
        /// <summary>
        /// Gets or sets the Certificate issuer for the certificate to talk to SF
        /// </summary>
        public string? CertIssuerNameForServiceFabricCert { get; set; }

        /// <summary>
        /// Gets or sets the name of the managed certificate
        /// </summary>
        public string? ManagedCertificateName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use Auth
        /// </summary>
        public bool UsesAuth { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of concurrent connections.
        /// This affects maximum number of concurrent connections of HttpClient which is used by client libraries we depend on, like Storage.
        /// Note that Kusto client depends on Storage client. Therefore, connection limit affects overall Kusto ingestion.
        /// </summary>
        public int MaxConcurrentConnections { get; set; } = 200;
        public abstract void Validate();
    }
}
