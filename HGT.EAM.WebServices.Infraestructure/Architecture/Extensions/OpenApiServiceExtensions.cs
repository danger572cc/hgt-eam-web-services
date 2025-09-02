using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Extensions;

public static class OpenApiServiceExtensions
{
    public static IServiceCollection AddConfigOpenApi(this IServiceCollection services, IConfiguration configuration)
    {
        var enableSchemeAuthScalar = !configuration.GetSection("EnableAuthScheme").Exists() ? false : configuration.GetSection("EnableAuthScheme").Get<bool>();
        if (!enableSchemeAuthScalar)
        {
            services.AddOpenApi(options => {
                options.AddDocumentTransformer((document, context, _) =>
                {
                    document.Info = new()
                    {
                        Title = configuration.GetSection("ApiTitle").Value,
                        Description = configuration.GetSection("ApiDescription").Value
                    };
                    return Task.CompletedTask;
                });
            });
        }
        else 
        {
            services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<BasicSecuritySchemeTransformer>();
            });
        }
        return services;
    }
}

internal sealed class BasicSecuritySchemeTransformer(
    Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider authenticationSchemeProvider
) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var settings = context.ApplicationServices.GetService<IConfiguration>();
        document.Info = new()
        {
            Title = settings?.GetSection("ApiTitle").Value,
            Description = settings?.GetSection("ApiDescription").Value
        };
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();

        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Basic"))
        {
            document.Components ??= new OpenApiComponents();

            var securitySchemeId = "Basic";

            if (!document.Components.SecuritySchemes.ContainsKey(securitySchemeId)) 
            {
                document.Components.SecuritySchemes.Add(securitySchemeId, new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "basic",
                    In = ParameterLocation.Header,
                    Description = "Basic Authorization header using the Bearer scheme.",
                });
             }

            document.SecurityRequirements.Add(new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecurityScheme
                    {
                        Reference =
                        new OpenApiReference
                        {
                            Id = securitySchemeId,
                            Type = ReferenceType.SecurityScheme
                        }
                    }
                ] = Array.Empty<string>()
            });
        }
    }
}