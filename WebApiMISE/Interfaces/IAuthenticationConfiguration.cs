using WebApiMISE.Configuration;

namespace WebApiMISE.Interfaces
{
    public interface IAuthenticationConfiguration
    {
        public IdentityConfiguration? IdentityConfiguration { get; set; }
    }
}
