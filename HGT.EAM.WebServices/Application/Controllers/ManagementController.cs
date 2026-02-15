using HGT.EAM.WebServices.Application.Queries;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.Controller;
using HGT.EAM.WebServices.Infrastructure.Architecture.Enums;
using HGT.EAM.WebServices.Infrastructure.Architecture.Models;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;
using System.Net;
using static HGT.EAM.WebServices.Infrastructure.Architecture.Enums.ApiFilterEnums;
using static HGT.EAM.WebServices.Infrastructure.Architecture.Enums.GridTypeEnums;

namespace HGT.EAM.WebServices.Application.Controllers;

[Authorize]
[Tags("Control de gestión")]
[ApiController]
[Route("api/management-control")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ManagementController : HGTController
{
    private readonly List<EAMGridSettings> _gridSettings;

    public ManagementController(
        IMediator mediator,
        ILogger<ManagementController> logger,
        List<EAMGridSettings> gridSettings
        )
        : base(mediator, logger)
    {
        _gridSettings = gridSettings.FindAll(filter => filter.HGTGridType == GridTypeEnums.HGTGridTypeEnum.ControlGestion);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("provisions")]
    [EndpointSummary("Grilla de provisiones 2.")]
    [EndpointDescription("Representa la grilla provisiones 2")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProvisionsAsync(
        [FromQuery]
        [Description("Tipo de filtro: 1 = dia anterior, 2 = Mes anterior, 3 = Mes actual, 4 = Año anterior, 5 = Mes y año en concreto")]
        ApiRequestEnum typeFilter,
        CancellationToken cancellationToken,
        [FromQuery]
        [Description("Mes en concreto a buscar, el rango de valores válidos es: 1-12")]
        int? month = null,
        [FromQuery]
        [Description("Año en concreto, valores validos a partir del año anterior")]
        int? year = null,
        [FromQuery]
        [Description("Número de página, se inicia con 1")]
        int page = 1,
        [FromQuery]
        [Description("Número de registros a obtener.")]
        int? pagSize = null)
    {
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.GrillaProvisiones);
        var query = new GridDataOnlyGetQuery(User, typeFilter, gridSettings, GridEnums.HGTGridEnum.GrillaProvisiones, GridTypeEnums.HGTGridTypeEnum.ControlGestion, page, pagSize, month, year);
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("maintenance/costs")]
    [EndpointSummary("Costos de mantenimiento.")]
    [EndpointDescription("Representa la grilla Información de Costos Mantenimiento (Resumen)")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMaintenanceCostsAsync(
        [FromQuery]
        [Description("Tipo de filtro: 1 = dia anterior, 2 = Mes anterior, 3 = Mes actual, 4 = Año anterior, 5 = Mes y año en concreto")]
        ApiRequestEnum typeFilter,
        CancellationToken cancellationToken,
        [FromQuery]
        [Description("Mes en concreto a buscar, el rango de valores válidos es: 1-12")]
        int? month = null,
        [FromQuery]
        [Description("Año en concreto, valores validos a partir del año anterior")]
        int? year = null,
        [FromQuery]
        [Description("Número de página, se inicia con 1")]
        int page = 1,
        [FromQuery]
        [Description("Número de registros a obtener.")]
        int? pagSize = null)
    {
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.CostosMantenimiento);
        var query = new GridDataOnlyGetQuery(User, typeFilter, gridSettings, GridEnums.HGTGridEnum.CostosMantenimiento, GridTypeEnums.HGTGridTypeEnum.ControlGestion, page, pagSize, month, year);
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }
}
