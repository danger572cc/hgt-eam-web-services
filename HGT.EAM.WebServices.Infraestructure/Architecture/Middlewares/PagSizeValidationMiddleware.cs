using Microsoft.AspNetCore.Http;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Middlewares;

public class PagSizeValidationMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Query.TryGetValue("pagSize", out var pageValues))
        {
            if (!int.TryParse(pageValues, out int pageNumber) || pageNumber <= 0)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid 'pageSize' query parameter. It must be a positive integer.");
                return;
            }

            if (pageNumber > 200) 
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsync("Invalid 'pageSize' query parameter. It must be less or equal than 200 records.");
                return;
            }
        }
        await _next(context);
    }
}
