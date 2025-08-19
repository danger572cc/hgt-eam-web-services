using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.Json;

namespace HGT.EAM.WebServices.Infraestructure.Architecture.Middlewares;

public class ResponseMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value?.ToLower();
        if (path == null || !path.StartsWith("/api"))
        {
            await _next(context);
            return;
        }

        var originalBodyStream = context.Response.Body;
        var memoryStream = new MemoryStream();
        context.Response.Body = memoryStream;

        try
        {
            await _next(context);

            if (context.Response.StatusCode == StatusCodes.Status204NoContent)
            {
                context.Response.Body = originalBodyStream;
                memoryStream.Dispose();
                return;
            }

            memoryStream.Seek(0, SeekOrigin.Begin);
            var responseBody = await new StreamReader(memoryStream).ReadToEndAsync();

            object? data = null;
            if (!string.IsNullOrWhiteSpace(responseBody))
            {
                var trimmed = responseBody.TrimStart();
                if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
                {
                    try
                    {
                        data = JsonSerializer.Deserialize<object>(
                            responseBody,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                    }
                    catch
                    {
                        data = responseBody;
                    }
                }
                else
                {
                    data = responseBody;
                }
            }

            int statusCode = context.Response.StatusCode;
            var message = GetStatusMessage(statusCode);

            var result = new
            {
                statusCode,
                message,
                data
            };

            context.Response.ContentType = "application/json";
            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
            await originalBodyStream.WriteAsync(Encoding.UTF8.GetBytes(json));
        }
        finally
        {
            context.Response.Body = originalBodyStream;
            memoryStream.Dispose();
        }
    }

    private static string GetStatusMessage(int statusCode)
    {
        return statusCode switch
        {
            200 => "OK",
            201 => "Created",
            204 => "No Content",
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            500 => "Internal Server Error",
            _ => "Unknown"
        };
    }
}
