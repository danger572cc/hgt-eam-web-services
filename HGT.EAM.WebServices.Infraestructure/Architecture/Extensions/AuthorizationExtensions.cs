using AspNetCore.Authentication.Basic;
using HGT.EAM.WebServices.Infrastructure.Architecture.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Extensions;

public static class AuthorizationExtensions
{
    public static WebApplicationBuilder AddBasicAuthorization(this WebApplicationBuilder builder)
    {
        builder.Services.AddBasicAuthorization(builder.Configuration);
        return builder;
    }

    public static IServiceCollection AddBasicAuthorization(this IServiceCollection services, IConfiguration configuration)
    {
        var credentialsSettings = configuration.GetSection("EAMCredentials");

        if (!credentialsSettings.Exists())
            throw new InvalidOperationException("EAMCredentials configuration section is missing.");

        List<EAMCredentialsSettings> allCredentials = configuration.GetSection("EAMCredentials").Get<List<EAMCredentialsSettings>>();

        if (allCredentials?.Count == 0)
            throw new InvalidOperationException("EAMCredentials configuration section is empty.");

        services.AddAuthentication(BasicDefaults.AuthenticationScheme)
            .AddBasic(options =>
            {
                options.Realm = "EAM-Webservices";
                options.Events = new BasicEvents
                {
                    OnValidateCredentials = async context =>
                    {
                        var userInfoEAM = allCredentials?.FirstOrDefault(f => f.Username == context.Username && f.Password == context.Password);

                        if (userInfoEAM != null)
                        {
                            var userClaims = new[]
                            {
                                new Claim(ClaimTypes.NameIdentifier, userInfoEAM.Username),
                                new Claim("Organization", userInfoEAM.Organization),
                                new Claim("Password", userInfoEAM.Password)
                            };
                            context.Principal = new ClaimsPrincipal(
                                new ClaimsIdentity(userClaims, context.Scheme.Name)
                            );
                            context.Success();
                        }
                        else
                        {
                            context.ValidationFailed();
                        }
                    }
                };
            });

        return services;
    }
}