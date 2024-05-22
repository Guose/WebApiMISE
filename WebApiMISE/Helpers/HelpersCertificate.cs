using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography.X509Certificates;

namespace WebApiMISE.Helpers
{
    public class HelpersCertificate
    {
        [ExcludeFromCodeCoverage]
        public static X509Certificate2 FindMatchingCertificateBySubject(string subjectCommonName)
        {
            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
                var certCollection = store.Certificates;
                var matchingCerts = new X509Certificate2Collection();

                foreach (var enumeratedCert in certCollection)
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(subjectCommonName, enumeratedCert.GetNameInfo(X509NameType.SimpleName, forIssuer: false))
                      && DateTime.Now < enumeratedCert.NotAfter
                      && DateTime.Now >= enumeratedCert.NotBefore
                      && (subjectCommonName.Equals("localhost", StringComparison.OrdinalIgnoreCase) || enumeratedCert.Verify())
                      )
                    {
                        matchingCerts.Add(enumeratedCert);
                    }
                }

                if (matchingCerts.Count == 0)
                {
                    throw new Exception($"Could not find a match for a certificate with subject 'CN={subjectCommonName}'.");
                }

                return matchingCerts[0];
            }
        }

        [ExcludeFromCodeCoverage]
        public static X509Certificate2 GetCertificate(StoreName storeName, StoreLocation location, string searchBy, X509FindType x509FindType)
        {
            List<Exception> exceptions = new List<Exception>();
            X509Certificate2? cert = default;

            using (var store = new X509Store(storeName, location))
            {
                store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);
                var collection = store.Certificates.Find(x509FindType, searchBy, true);

                if (collection.Count == 0)
                {
                    throw new InvalidOperationException($"No certificate with {x509FindType} {searchBy} found.");
                }

                if ((x509FindType == X509FindType.FindByThumbprint) || (collection.Count == 1))
                {
                    try
                    {
                        if (collection[0].Verify())
                        {
                            cert = collection[0];
                        }
                    }
                    catch (Exception e)
                    {
                        exceptions.Add(e);
                    }
                }
                else
                {
                    // Find the first non-expired one and return it.
                    foreach (var c in collection)
                    {
                        try
                        {
                            if (c.Verify())
                            {
                                cert = c;
                                break;
                            }
                        }
                        catch (Exception e)
                        {
                            exceptions.Add(e);
                        }
                    }
                }

                if (cert == default)
                {
                    if (exceptions.Any())
                    {
                        throw new AggregateException($"Certificate Search by: {searchBy} with FindType: {x509FindType} Found 0 Certificates in: {storeName} at: {location}.", exceptions);
                    }
                    else
                    {
                        throw new AggregateException($"Certificate Search by: {searchBy} with FindType: {x509FindType} Found 0 Certificates in: {storeName} at: {location}.", new InvalidOperationException($"Certificate Not found."));
                    }
                }

                return cert;
            }
        }
    }
}
