using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace HGT.EAM.WebServices.Infraestructure.Architecture.Extensions;

public static class OpenApiServiceExtensions
{
    public static IServiceCollection AddConfigOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi("v1", options =>
        {
            options.AddDocumentTransformer<BasicSecuritySchemeTransformer>();
        });

        return services;
    }
}

internal sealed class BasicSecuritySchemeTransformer(
    Microsoft.AspNetCore.Authentication.IAuthenticationSchemeProvider authenticationSchemeProvider
) : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        var authenticationSchemes = await authenticationSchemeProvider.GetAllSchemesAsync();

        if (authenticationSchemes.Any(authScheme => authScheme.Name == "Basic"))
        {
            document.Components ??= new OpenApiComponents();

            var securitySchemeId = "Basic";

            document.Components.SecuritySchemes.Add(securitySchemeId, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "basic",
                In = ParameterLocation.Header,
                Description = "Basic Authorization header using the Bearer scheme."
            });

            document.SecurityRequirements.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Id = securitySchemeId,
                            Type = ReferenceType.SecurityScheme
                        }
                }] = Array.Empty<string>()
            });
        }
    }
}