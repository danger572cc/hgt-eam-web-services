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
        _gridSettings = gridSettings.FindAll(filter => filter.HGTGridType == GriTypeEnums.HGTGridTypeEnum.ControlGestion);
    }

    [HttpGet("provisions")]
    [EndpointSummary("Grilla de provisiones 2.")]
    [EndpointDescription("Representa la grilla Grilla provisiones 2")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProvisionsAsync(
        [FromQuery]
        [Description("Tipo de filtro: 1 = dia anterior, 2 = Mes anterior, 3 = Año anterior o últimos 12 meses")]
        ApiRequestEnum typeFilter,
        CancellationToken cancellationToken,
        [FromQuery]
        [Description("Número de página, se inicia con 1")]
        int page = 1)
    {
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.GrillaProvisiones);
        var query = new GridDataOnlyGetQuery
        {
            Username = User.Identity.Name,
            Password = User.Claims.FirstOrDefault(i => i.Type == "Password")?.Value,
            Organization = User.Claims.FirstOrDefault(i => i.Type == "Organization")?.Value,
            FunctionName = gridSettings.UserFunction,
            GridName = gridSettings.GridName,
            GridId = gridSettings.GridId,
            Page = page,
            NumberOfRowsFirstReturned = gridSettings.NumberRecordsFirstReturned,
            DataspyId = typeFilter switch
            {
                ApiRequestEnum.Day => gridSettings.DataSpyIds.Day,
                ApiRequestEnum.Month => gridSettings.DataSpyIds.Month,
                ApiRequestEnum.Year => gridSettings.DataSpyIds.Year,
                ApiRequestEnum.Custom => gridSettings.DataSpyIds.Custom,
                _ => throw new InvalidOperationException("Invalid filter, accepted values ​​are: 1 = day, 2 = month, 3 = year, 4 = custom."),
            },
            GridHGT = GridEnums.HGTGridEnum.GrillaProvisiones,
            GridTypeHGT = GriTypeEnums.HGTGridTypeEnum.ControlGestion
        };
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }

    [HttpGet("maintenance/costs")]
    [EndpointSummary("Costos de mantenimiento.")]
    [EndpointDescription("Representa la grilla Información de Costos Mantenimiento (Resumen)")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMaintenanceCostsAsync(
        [FromQuery]
        [Description("Tipo de filtro: 1 = dia anterior, 2 = Mes anterior, 3 = Año anterior o últimos 12 meses")]
        ApiRequestEnum typeFilter,
        CancellationToken cancellationToken,
        [FromQuery]
        [Description("Número de página, se inicia con 1")]
        int page = 1)
    {
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.CostosMantenimiento);
        var query = new GridDataOnlyGetQuery
        {
            Username = User.Identity.Name,
            Password = User.Claims.FirstOrDefault(i => i.Type == "Password")?.Value,
            Organization = User.Claims.FirstOrDefault(i => i.Type == "Organization")?.Value,
            FunctionName = gridSettings.UserFunction,
            GridName = gridSettings.GridName,
            GridId = gridSettings.GridId,
            Page = page,
            NumberOfRowsFirstReturned = gridSettings.NumberRecordsFirstReturned,
            DataspyId = typeFilter switch
            {
                ApiRequestEnum.Day => gridSettings.DataSpyIds.Day,
                ApiRequestEnum.Month => gridSettings.DataSpyIds.Month,
                ApiRequestEnum.Year => gridSettings.DataSpyIds.Year,
                ApiRequestEnum.Custom => gridSettings.DataSpyIds.Custom,
                _ => throw new InvalidOperationException("Invalid filter, accepted values ​​are: 1 = day, 2 = month, 3 = year, 4 = custom."),
            },
            GridHGT = GridEnums.HGTGridEnum.CostosMantenimiento,
            GridTypeHGT = GriTypeEnums.HGTGridTypeEnum.ControlGestion
        };
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }
}
