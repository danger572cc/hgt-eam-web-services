using HGT.EAM.WebServices.Infraestructure.Architecture.Exceptions;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace HGT.EAM.WebServices.Infraestructure.Architecture.Middlewares;

public class ExceptionMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

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

    private static Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        if (context.Response.HasStarted)
        {
            return Task.CompletedTask;
        }

        int statusCode = 500;
        string message = "Ha ocurrido un error interno.";
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
                    validationErrors
                };
                break;

            case OCTNotFoundException notFoundEx:
                statusCode = 404;
                message = notFoundEx.Message;
                response = new
                {
                    statusCode,
                    message
                };
                break;

            case UnauthorizedAccessException:
                statusCode = 401;
                message = "No autorizado.";
                response = new
                {
                    statusCode,
                    message
                };
                break;

            case ArgumentOutOfRangeException or ArgumentNullException or ArgumentException or InvalidOperationException:
                statusCode = 400;
                message = ex.Message;
                response = new
                {
                    statusCode,
                    message
                };
                break;

            case NotSupportedException:
                statusCode = 501;
                message = ex.Message;
                response = new
                {
                    statusCode,
                    message
                };
                break;

            case TimeoutException:
                statusCode = 408;
                message = ex.Message;
                response = new
                {
                    statusCode,
                    message
                };
                break;

            default:
                response = new
                {
                    statusCode,
                    message
                };
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = statusCode;
        context.Response.Headers["X-Exception-Occurred"] = "true";

        var serializeOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        return context.Response.WriteAsync(JsonSerializer.Serialize(response, serializeOptions));
    }
}