using System.Security.Cryptography.X509Certificates;
using WebApiMISE.Interfaces;

namespace WebApiMISE.CertificateHelpers
{
    public class KeyValutManager : IKeyVaultManager
    {
        public X509Certificate2 FindCertificateByName(string certificateName)
        {
            throw new NotImplementedException();
        }

        public string GetSecret(string name)
        {
            throw new NotImplementedException();
        }
    }
}
