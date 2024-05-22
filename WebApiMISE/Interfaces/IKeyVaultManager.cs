using System.Security.Cryptography.X509Certificates;

namespace WebApiMISE.Interfaces
{
    public interface IKeyVaultManager
    {
        X509Certificate2 FindCertificateByName(string certificateName);

        string GetSecret(string name);
    }
}
