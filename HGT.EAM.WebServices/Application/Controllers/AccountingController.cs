using HGT.EAM.WebServices.Application.Models;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.Enums;
using HGT.EAM.WebServices.Infrastructure.Architecture.Models;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HGT.EAM.WebServices.Application.Controllers;

[Authorize]
[Tags("Contabilidad")]
[ApiController]
[Route("api/accounting")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[ResponseCache(Duration = 900)]
public class AccountingController : BaseGridController
{
    public AccountingController(
        IMediator mediator,
        ILogger<AccountingController> logger,
        List<EAMGridSettings> gridSettings
        )
        : base(mediator, logger, gridSettings)
    {
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("transactions")]
    [EndpointSummary("Grilla de transacciones.")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionsAsync(
        [FromQuery] GridRequestParams request,
        CancellationToken cancellationToken)
    {
        return await ExecuteGridQuery(
            GridEnums.HGTGridEnum.GrillaTransacciones,
            GridTypeEnums.HGTGridTypeEnum.Contabilidad,
            request,
            cancellationToken);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("kardex")]
    [EndpointSummary("Informe de Kardex.")]
    [EndpointDescription("Representa al informe de cognos informe de kardex")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKardexAsync(
        [FromQuery] GridRequestParams request,
        CancellationToken cancellationToken)
    {
        return await ExecuteGridQuery(
            GridEnums.HGTGridEnum.Kardex,
            GridTypeEnums.HGTGridTypeEnum.Contabilidad,
            request,
            cancellationToken);
    }
}
