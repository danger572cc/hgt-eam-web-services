using Microsoft.AspNetCore.Http;
using static HGT.EAM.WebServices.Infrastructure.Architecture.Enums.ApiFilterEnums;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Middlewares;

public class QueryParamsValidationMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    private const int FIRST_MONTH = 1;

    private const int LAST_MONTH = 12;

    private readonly int startYear = DateTime.Now.AddYears(-1).Year;

    public async Task InvokeAsync(HttpContext context)
    {
        try 
        {
            IsInvalidQueryParam(context.Request.Query);
            IsInvalidFilterByRange(context.Request.Query);
            await _next(context);
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(ex.Message);
            return;
        }
    }

    private void IsInvalidQueryParam(IQueryCollection queryParams) 
    {
        bool isInvalid = false;
        string message = string.Empty;
        foreach (var queryParam in queryParams)
        {
            var queryParamKey = queryParam.Key;
            var queryParamValue = queryParam.Value;
            switch (queryParamKey)
            {
                case "pagSize":
                    isInvalid = !int.TryParse(queryParamValue, out int pageNumber) || pageNumber <= 0;
                    if (isInvalid)
                    {
                        message = "Invalid 'pagSize' query parameter. It must be a positive integer.";
                    }
                    else if (pageNumber > 500)
                    {
                        message = "Invalid 'pagSize' query parameter. It must be less or equal than 500 records.";
                    }
                    break;
                case "month":
                    isInvalid = !int.TryParse(queryParamValue, out int month) || month <= 0;
                    if (isInvalid)
                    {
                        message = "Invalid 'pagSize' query parameter. It must be a positive integer.";
                    }
                    else if (FIRST_MONTH >= 1 && LAST_MONTH <= 12)
                    {
                        message = "Invalid 'month' query parameter. The valid values ​​for the months are: [1-12].";
                    }

                    break;
                case "year":
                    isInvalid = !int.TryParse(queryParamValue, out int year) || year <= 0;
                    if (isInvalid)
                    {
                        message = "Invalid 'year' query parameter. It must be a positive integer.";
                    }
                    else if (year < startYear)
                    {
                        message = $"Invalid 'year' query parameter. The year must be greater than or equal to: {startYear}.";
                    }
                    break;
            }
        }
        if (isInvalid) 
        {
            throw new InvalidOperationException(message);
        }
    }

    private void IsInvalidFilterByRange(IQueryCollection queryParams) 
    {
        bool isInvalid = false;
        string message = string.Empty;
        if (queryParams.TryGetValue("typeFilter", out var typeFilterValues))
        {
            if ((Enum.TryParse(typeFilterValues, out ApiRequestEnum typeFilterValue) || typeFilterValue == ApiRequestEnum.FullMonthByYear) && (!queryParams.ContainsKey("month") && !queryParams.ContainsKey("year")))
            {
                message = "If the typeFilter is 5, you must indicate the month and year to consult.";
                isInvalid = true;
            }
        }
        if (isInvalid)
        {
            throw new InvalidOperationException(message);
        }
    }
}
