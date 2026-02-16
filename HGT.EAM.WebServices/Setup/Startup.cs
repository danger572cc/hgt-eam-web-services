using HGT.EAM.WebServices.Application.Mapper;
using HGT.EAM.WebServices.Infrastructure.Architecture.Extensions;
using HGT.EAM.WebServices.Infrastructure.Architecture.GridCache;
using HGT.EAM.WebServices.Infrastructure.Architecture.Middlewares;
using Mapster;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using System.Threading.RateLimiting;

namespace HGT.EAM.WebServices.Setup;

public class Startup(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public void Configure(WebApplication app)
    {
        using (var scope = app.Services.CreateScope())
        {
            var gridCacheDb = scope.ServiceProvider.GetRequiredService<GridCacheDbContext>();
            gridCacheDb.Database.EnsureCreated();
        }

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
        app.UseRateLimiter();
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
                string user = (httpContext.User?.Identity?.IsAuthenticated == true ? httpContext.User.Identity.Name : null) ?? "Anonymous";
                diagnosticContext.Set("CurrentUser", user);
            };
        });
        app.UseMiddleware<ExceptionMiddleware>()
            .UseMiddleware<ResponseMiddleware>()
            .UseMiddleware<QueryParamsValidationMiddleware>();
        app.UseResponseCaching();
        app.MapControllers().RequireRateLimiting("api");
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                if (context.HttpContext.Response.HasStarted)
                {
                    return;
                }

                context.HttpContext.Response.ContentType = "application/json";

                var retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterValue)
                    ? (int)Math.Ceiling(retryAfterValue.TotalSeconds)
                    : (int?)null;

                if (retryAfter is not null)
                {
                    context.HttpContext.Response.Headers.RetryAfter = retryAfter.Value.ToString();
                }

                await context.HttpContext.Response.WriteAsync(
                    $"{{\"statusCode\":429,\"message\":\"Too many requests\",\"retryAfterSeconds\":{(retryAfter is null ? "null" : retryAfter.Value.ToString())}}}",
                    token);
            };

            options.AddPolicy("api", httpContext =>
            {
                // Solo para endpoints de API.
                if (!httpContext.Request.Path.StartsWithSegments("/api"))
                {
                    return RateLimitPartition.GetNoLimiter("non-api");
                }

                string key;

                if (httpContext.User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(httpContext.User.Identity.Name))
                {
                    key = $"user:{httpContext.User.Identity.Name}";
                }
                else
                {
                    var ip = httpContext.Connection.RemoteIpAddress;
                    key = ip is null ? "ip:unknown" : $"ip:{ip}";
                }

                // Ventana fija: 60 requests por minuto por usuario (o por IP si anónimo).
                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: key,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
            });
        });

        services.AddApplicationServices(configuration);
        services.AddConfigOpenApi(configuration);
        services.AddMapster();
        MapsterConfig.Configure();
        services.AddMediator();
        services.AddMemoryCache();
        services.AddResponseCaching();
    }
}