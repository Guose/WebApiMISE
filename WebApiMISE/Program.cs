
using Azure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Identity.Client;
using Microsoft.Identity.ServiceEssentials;
using Microsoft.Identity.ServiceEssentials.Extensions.AspNetCoreMiddleware;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.S2S.Extensions.AspNetCore;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Runtime.Caching;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using WebApiMISE.Configuration;
using WebApiMISE.Helpers;
using WebApiMISE.Interfaces;
using WebApiMISE.ProviderHelpers;
using HeaderNames = Microsoft.Net.Http.Headers.HeaderNames;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;

namespace WebApiMISE
{
    public class Program
    {
        private readonly IKeyVaultManager? keyVaultManager;
        private readonly MemoryCache? cache;

        private readonly string[] scopes
            = new[]
            {
                "User.Read",
                "User.ReadBasic.All",
                "GroupMember.Read.All"
            };

        private readonly string[] memberGroups
            = new[]
            {
                "485f96b8-4f1a-4742-933d-9a55188d7607",
                "f988e922-0612-4b09-8ec0-d31bca3250ca"
            };

        private bool disposedValue;

        public GatewayServiceConfiguration? GatewayConfig { get; }

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            var program = new Program();

            var principalProvider = new ClaimsPrincipalProvider();

            builder.Services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.UseSecurityTokenValidators = true;
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = async ctx =>
                    {
                        var request = ctx.HttpContext.Request;
                        var claims = new List<Claim>();
                        var authorization = request.Headers[HeaderNames.Authorization];
                        if (AuthenticationHeaderValue.TryParse(authorization, out var oboToken))
                        {
                            var assertion = new UserAssertion(oboToken.Parameter);
                            var confidentialClient = program.GetConfidentialClientApplication();
                            var result = await confidentialClient.AcquireTokenOnBehalfOf(program.scopes, assertion).ExecuteAsync();

                            request.Headers.Authorization = new Microsoft.Extensions.Primitives.StringValues(result.AccessToken.ToString());

                            var options = new DeviceCodeCredentialOptions
                            {
                                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,

                                DeviceCodeCallback = (code, cancellation) =>
                                {
                                    Console.WriteLine(code.Message);
                                    return Task.FromResult(0);
                                },
                            };

                            var deviceCodeCredential = new DeviceCodeCredential(options);

                            var graphClient = new GraphServiceClient(deviceCodeCredential, program.scopes);

                            // Retrieve the user's group memberships .Request().GetAsync();
                            var userMemberOf = await graphClient.Me.MemberOf.Request().GetAsync();

                            // Iterate through the groups and check for membership
                            foreach (var directoryObject in userMemberOf)
                            {
                                if (directoryObject is Group group)
                                {
                                    // Check if the user is a member of the IncidentCommunicationsManagement group
                                    if (string.Equals(group.Id, Constants.GroupIds.IncidentCommunicationsManagementGroupId, StringComparison.OrdinalIgnoreCase))
                                    {
                                        claims.Add(new Claim(ClaimsIdentity.DefaultRoleClaimType, "IncidentManager"));
                                    }
                                    else if (string.Equals(group.Id, Constants.GroupIds.HermesUserGroupId, StringComparison.OrdinalIgnoreCase))
                                    {
                                        claims.Add(new Claim(ClaimsIdentity.DefaultRoleClaimType, "HermesUser"));
                                    }
                                }
                            }

                        }
                        else
                        {
                            var handler = new JwtSecurityTokenHandler();
                            var jwtToken = handler.ReadJwtToken(oboToken!.Parameter);
                            var hermesUserRoleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "roles" && c.Value == "HermesUser");
                            var incidentManagerRoleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "roles" && c.Value == "IncidentManager");

                            if (hermesUserRoleClaim != null || incidentManagerRoleClaim != null)
                            {
                                if (hermesUserRoleClaim != null)
                                {
                                    claims.Add(new Claim(ClaimsIdentity.DefaultRoleClaimType, "HermesUser"));
                                }

                                if (incidentManagerRoleClaim != null)
                                {
                                    claims.Add(new Claim(ClaimsIdentity.DefaultRoleClaimType, "IncidentManager"));
                                }
                            }
                        }
                    }
                };
            });


            // Add services to the container.
            builder.Services
                .AddAuthentication(S2SAuthenticationDefaults.AuthenticationScheme)
                .AddMiseWithDefaultAuthentication(builder.Configuration)
                .EnableTokenAcquisitionToCallDownstreamApiAndDataProviderAuthentication(S2SAuthenticationDefaults.AuthenticationScheme)
                .AddInMemoryTokenCaches();

            builder.Services.AddRequiredScopeAuthorization();
            builder.Services.AddRequiredScopeOrAppPermissionAuthorization();

            builder.Services.AddAuthorization(options =>
            {
                var defaultAuthorizationPolicyBuilder = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme);
                defaultAuthorizationPolicyBuilder = defaultAuthorizationPolicyBuilder.RequireAuthenticatedUser();
                options.DefaultPolicy = defaultAuthorizationPolicyBuilder.Build();
                options.AddPolicy("HermesUser", policy => policy.RequireRole("HermesUser", "Admin", "admin"));
                options.AddPolicy("IncidentManager", policy => policy.RequireRole("IncidentManager", "Admin", "admin"));
            });

            builder.Services.AddScoped<WeatherForecast>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.UseMise();

            app.MapControllers();

            app.Run();
        }


        public IConfidentialClientApplication GetConfidentialClientApplication()
        {
            IConfidentialClientApplication? client = cache!.Get(nameof(this.GetConfidentialClientApplication)) as IConfidentialClientApplication;
            if (client != null)
            {
                return client;
            }

            X509Certificate2 certificate;
            if (GatewayConfig?.ManagedCertificateName == "localhost")
            {
                certificate = HelpersCertificate.FindMatchingCertificateBySubject(GatewayConfig.IdentityConfiguration!.CertificateSubjectName!);
            }
            else
            {
                certificate = keyVaultManager!.FindCertificateByName(GatewayConfig!.ManagedCertificateName!);
            }

            client = ConfidentialClientApplicationBuilder
                .Create(this.GatewayConfig.AuthenticationConfiguration.ClientId)
                .WithTenantId(this.GatewayConfig.AuthenticationConfiguration.TenantId)
                .WithCertificate(certificate)
                .Build();

            cache.Add(
                new CacheItem(nameof(this.GetConfidentialClientApplication), client),
                new CacheItemPolicy()
                {
                    AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(15)
                });

            return client;
        }
    }
}
