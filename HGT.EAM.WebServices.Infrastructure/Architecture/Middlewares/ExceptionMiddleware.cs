using HGT.EAM.WebServices.Infrastructure.Architecture.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Middlewares;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ExceptionMiddleware> _logger = logger;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        var correlationId = GetOrCreateCorrelationId(context);
        var env = context.RequestServices.GetRequiredService<Microsoft.AspNetCore.Hosting.IHostingEnvironment>();
        var isDevelopment = env.IsDevelopment();

        // Logging estructurado
        _logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}, Path: {Path}, User: {User}",
            correlationId,
            context.Request.Path,
            context.User?.Identity?.Name ?? "Anonymous");

        int statusCode = 500;
        string message = isDevelopment ? $"Internal error. Details: {ex.Message}" : "An unexpected error occurred. Please try again later.";
        object response;

        switch (ex)
        {
            case OCTBadRequestException badRequestEx when badRequestEx.ValidationErrors is { Count: > 0 } validationErrors:
                statusCode = 400;
                message = badRequestEx.Message;
                response = new
                {
                    statusCode,
                    message,
                    validationErrors,
                    correlationId
                };
                break;

            case OCTNotFoundException notFoundEx:
                statusCode = 404;
                message = notFoundEx.Message;
                response = new
                {
                    statusCode,
                    message,
                    correlationId
                };
                break;

            case UnauthorizedAccessException:
                statusCode = 401;
                message = "Unauthorized.";
                response = new
                {
                    statusCode,
                    message,
                    correlationId
                };
                break;

            case ArgumentOutOfRangeException or ArgumentNullException or ArgumentException or InvalidOperationException:
                statusCode = 400;
                message = ex.Message;
                response = new
                {
                    statusCode,
                    message,
                    correlationId
                };
                break;

            case NotSupportedException:
                statusCode = 501;
                message = ex.Message;
                response = new
                {
                    statusCode,
                    message,
                    correlationId
                };
                break;

            case TimeoutException:
                statusCode = 408;
                message = ex.Message;
                response = new
                {
                    statusCode,
                    message,
                    correlationId
                };
                break;

            default:
                response = new
                {
                    statusCode,
                    message,
                    correlationId
                };
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        context.Response.Headers["X-Exception-Occurred"] = "true";
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, serializeOptions));
    }

    private static string GetOrCreateCorrelationId(HttpContext context)
    {
        const string headerName = "X-Correlation-Id";
        if (context.Request.Headers.TryGetValue(headerName, out var correlationId))
        {
            return correlationId!;
        }

        var newCorrelationId = Guid.NewGuid().ToString();
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[headerName] = newCorrelationId;
            return Task.CompletedTask;
        });

        return newCorrelationId;
    }
}