using System.Security.Claims;
using System.Security.Principal;
using WebApiMISE.Interfaces;

namespace WebApiMISE.ProviderHelpers
{
    public class ClaimsPrincipalProvider : IPrincipalProvider
    {
        public IPrincipal? GetCurrentPrincipal() => ClaimsPrincipal.Current;
    }
}
