using HGT.EAM.WebServices.Application.Models;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.Enums;
using HGT.EAM.WebServices.Infrastructure.Architecture.Models;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HGT.EAM.WebServices.Application.Controllers;

[Authorize]
[Tags("Control de gestión")]
[ApiController]
[Route("api/management-control")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ManagementController : BaseGridController
{
    public ManagementController(
        IMediator mediator,
        ILogger<ManagementController> logger,
        List<EAMGridSettings> gridSettings
        )
        : base(mediator, logger, gridSettings)
    {
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("provisions")]
    [EndpointSummary("Grilla de provisiones 2.")]
    [EndpointDescription("Representa la grilla provisiones 2")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProvisionsAsync(
        [FromQuery] GridRequestParams request,
        CancellationToken cancellationToken)
    {
        return await ExecuteGridQuery(
            GridEnums.HGTGridEnum.GrillaProvisiones,
            GridTypeEnums.HGTGridTypeEnum.ControlGestion,
            request,
            cancellationToken);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("maintenance/costs")]
    [EndpointSummary("Costos de mantenimiento.")]
    [EndpointDescription("Representa la grilla Información de Costos Mantenimiento (Resumen)")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMaintenanceCostsAsync(
        [FromQuery] GridRequestParams request,
        CancellationToken cancellationToken)
    {
        return await ExecuteGridQuery(
            GridEnums.HGTGridEnum.CostosMantenimiento,
            GridTypeEnums.HGTGridTypeEnum.ControlGestion,
            request,
            cancellationToken);
    }
}
