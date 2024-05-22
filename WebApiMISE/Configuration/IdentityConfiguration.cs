using Azure.Core;
using Azure.Identity;
using Azure;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using WebApiMISE.Interfaces;
using WebApiMISE.Types;

namespace WebApiMISE.Configuration
{
    public class IdentityConfiguration : IValidatable
    {
        /// <summary>
        /// Gets or sets ClientAuthType - identifies if SasTokenOrSecretName is a Sas Token or a Secret Name
        /// </summary>
        public ClientAuthType ClientAuthType { get; set; }

        /// <summary>
        /// Gets or sets managed identity client id
        /// </summary>
        public string? ManagedIdentityClientId { get; set; }

        /// <summary>
        /// Gets or sets the Sas Token or Secret Name
        /// </summary>
        public string? SasTokenOrSecretName { get; set; }

        /// <summary>
        /// Gets or sets certificate subject name
        /// </summary>
        public string? CertificateSubjectName { get; set; }

        /// <summary>
        /// Gets or sets Tenant Id
        /// </summary>
        public string? TenantId { get; set; }

        /// <summary>
        /// Gets or sets Client Id
        /// </summary>
        public string? ClientId { get; set; }

        public void Validate()
        {
            if (ClientAuthType == ClientAuthType.ManagedIdentity && string.IsNullOrEmpty(ManagedIdentityClientId))
            {
                throw new ArgumentNullException($"{nameof(ManagedIdentityClientId)} isn't set");
            }
            else if ((ClientAuthType == ClientAuthType.Key || ClientAuthType == ClientAuthType.KeyIdentity) && string.IsNullOrEmpty(SasTokenOrSecretName))
            {
                throw new ArgumentNullException($"{nameof(SasTokenOrSecretName)} isn't set");
            }
            else if (ClientAuthType == ClientAuthType.Certificate)
            {
                if (string.IsNullOrEmpty(CertificateSubjectName))
                {
                    throw new ArgumentNullException($"{nameof(CertificateSubjectName)} isn't set");
                }

                if (string.IsNullOrEmpty(ClientId))
                {
                    throw new ArgumentNullException($"{nameof(ClientId)} isn't set");
                }

                if (string.IsNullOrEmpty(TenantId))
                {
                    throw new ArgumentNullException($"{nameof(TenantId)} isn't set");
                }
            }
        }

        public TokenCredential CreateTokenCredential()
        {
            if (ClientAuthType == ClientAuthType.Key || ClientAuthType == ClientAuthType.KeyIdentity)
            {
                throw new NotSupportedException();
            }
            else if (ClientAuthType == ClientAuthType.Certificate)
            {
                var cert = GetCertificate(CertificateSubjectName!);
                return new ClientCertificateCredential(TenantId, ClientId, cert);
            }
            else
            {
                return new DefaultAzureCredential(new DefaultAzureCredentialOptions() { ManagedIdentityClientId = ManagedIdentityClientId });
            }
        }

        public AzureSasCredential CreateSasCredential(IKeyVaultManager kvManager)
        {
            Debug.Assert(kvManager != null, "kvmanager is null");

            if (ClientAuthType == ClientAuthType.Key || ClientAuthType == ClientAuthType.KeyIdentity)
            {
                var secret = kvManager.GetSecret(SasTokenOrSecretName!);
                return new AzureSasCredential(secret);
            }

            throw new NotSupportedException();
        }

        [ExcludeFromCodeCoverage]
        private static X509Certificate2? GetCertificate(string subjectName)
        {
            if (string.IsNullOrEmpty(subjectName))
            {
                return default;
            }

            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                var collection = store.Certificates.Find(X509FindType.FindBySubjectName, subjectName, true);
                if (collection?.Count >= 1)
                {
                    return collection[0];
                }

                return default;
            }
        }
    }
}
