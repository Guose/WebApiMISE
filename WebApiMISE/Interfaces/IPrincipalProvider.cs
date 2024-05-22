using System.Security.Principal;

namespace WebApiMISE.Interfaces
{
    public interface IPrincipalProvider
    {
        IPrincipal? GetCurrentPrincipal();
    }
}
