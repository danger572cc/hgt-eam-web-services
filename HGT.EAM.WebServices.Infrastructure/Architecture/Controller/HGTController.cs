using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Runtime.CompilerServices;

namespace HGT.EAM.WebServices.Infrastructure.Architecture.Controller;

public class HGTController(IMediator mediator, ILogger logger) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly ILogger _logger = logger;

    protected async Task<IActionResult> ExecuteHandler<TRequest, TResponse>(
        TRequest request,
        HttpStatusCode statusCode = HttpStatusCode.OK,
        CancellationToken cancellationToken = default,
        [CallerMemberName] string callerMemberName = ""
    ) where TRequest : IRequest<TResponse>
    {
        try
        {
            var result = await _mediator.Send(request, cancellationToken);
            return StatusCode((int)statusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in {CallerMemberName}", callerMemberName);
            throw;
        }
    }
}
