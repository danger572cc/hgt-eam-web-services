using HGT.EAM.WebServices.Application.Mapper;
using HGT.EAM.WebServices.Infrastructure.Architecture.Extensions;
using HGT.EAM.WebServices.Infrastructure.Architecture.Middlewares;
using Mapster;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System.Reflection;

namespace HGT.EAM.WebServices.Setup;

public class Startup(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public void Configure(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error");
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "HGT Grid API - EAM";
            options.Layout = ScalarLayout.Modern;
            options.ShowSidebar = true;
            options.DarkMode = false;
            options.HideDarkModeToggle = false;
            options.DefaultOpenAllTags = true;
            options.Favicon = "/images/favicon.ico";
            options.HeadContent = @"<div style='position:fixed;top:0;left:0;width:100%;height:4.5%;z-index:100;color:white;background-color: #222A36 !important;'><a href='https://www.aep.cl/'><img src='https://www.aep.cl/wp-content/uploads/2025/07/logoHGT-blanco.png' alt='Hanseatic Global Terminals' style='width: 7%;margin-top: 5px;'></a></div>";
        });
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var user = context.User?.Identity?.IsAuthenticated == true
                ? context.User.Identity.Name
                : "Anonymous";

            var method = context.Request.Method;
            var queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;

            using (Serilog.Context.LogContext.PushProperty("RequestPath", path))
            using (Serilog.Context.LogContext.PushProperty("CurrentUser", user))
            using (Serilog.Context.LogContext.PushProperty("QueryString", queryString))
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    Log.Information($"Current environment: {app.Environment.EnvironmentName}");
                    Log.Information("Invoking endpoint {Method} {Path} Query={QueryString}", method, path, queryString);
                }
                await next();
            }

        });

        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate = "Request {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms by {CurrentUser}";

            options.GetLevel = (httpContext, elapsed, ex) =>
            {
                if (ex != null)
                    return LogEventLevel.Error;

                var path = httpContext.Request.Path.Value ?? string.Empty;
                return path.Contains("/api", StringComparison.OrdinalIgnoreCase)
                    ? LogEventLevel.Information
                    : LogEventLevel.Verbose;
            };

            options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
            {
                diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value ?? string.Empty);
                string? user = httpContext.User?.Identity?.IsAuthenticated == true ? httpContext.User.Identity.Name
                    : "Anonymous";
                diagnosticContext.Set("CurrentUser", user);
            };
        });
        app.UseMiddleware<ExceptionMiddleware>()
            .UseMiddleware<ResponseMiddleware>()
            .UseMiddleware<QueryParamsValidationMiddleware>();
        app.UseResponseCaching();
        app.MapControllers();
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();
        services.AddApplicationServices(configuration);
        services.AddConfigOpenApi(configuration);
        services.AddMapster();
        MapsterConfig.Configure();
        services.AddMediator((Mediator.MediatorOptions options) =>
        {
            options.ServiceLifetime = ServiceLifetime.Singleton;
            options.GenerateTypesAsInternal = true;
            options.Assemblies = [Assembly.GetExecutingAssembly()];
        });
        services.AddMemoryCache();
        services.AddResponseCaching();
    }
}