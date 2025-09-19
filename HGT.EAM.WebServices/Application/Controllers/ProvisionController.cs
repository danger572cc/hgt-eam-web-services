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
[Tags("Abastecimiento")]
[ApiController]
[Route("api/provisions")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ProvisionController : HGTController
{
    private readonly List<EAMGridSettings> _gridSettings;

    public ProvisionController(
        IMediator mediator,
        ILogger<ProvisionController> logger,
        List<EAMGridSettings> gridSettings
        )
        : base(mediator, logger)
    {
        _gridSettings = gridSettings.FindAll(filter => filter.HGTGridType == GriTypeEnums.HGTGridTypeEnum.Abastecimiento);
    }

    [HttpGet("contracts")]
    [EndpointSummary("Datos generales del contrato.")]
    [EndpointDescription("Representa la grilla Datos generales del contrato")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInfoContractsAsync(
        [FromQuery]
        [Description("Tipo de filtro: 1 = dia anterior, 2 = Mes actual, 3 = Año anterior o últimos 12 meses")]
        ApiRequestEnum typeFilter,
        CancellationToken cancellationToken,
        [FromQuery]
        [Description("Número de página, se inicia con 1")]
        int page = 1,
        [FromQuery]
        [Description("Número de registros a obtener.")]
        int? pagSize = null)
    {
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.DatosGeneralesContrato);
        var query = new GridDataOnlyGetQuery
        {
            Username = User.Identity.Name,
            Password = User.Claims.FirstOrDefault(i => i.Type == "Password")?.Value,
            Organization = User.Claims.FirstOrDefault(i => i.Type == "Organization")?.Value,
            FunctionName = gridSettings.UserFunction,
            GridName = gridSettings.GridName,
            GridId = gridSettings.GridId,
            Page = page,
            NumberOfRowsFirstReturned = !pagSize.HasValue ? gridSettings.NumberRecordsFirstReturned : pagSize.GetValueOrDefault(),
            DataspyId = typeFilter switch
            {
                ApiRequestEnum.Day => gridSettings.DataSpyIds.Day,
                ApiRequestEnum.Month => gridSettings.DataSpyIds.Month,
                ApiRequestEnum.Year => gridSettings.DataSpyIds.Year,
                ApiRequestEnum.Custom => gridSettings.DataSpyIds.Custom,
                _ => throw new InvalidOperationException("Invalid filter, accepted values ​​are: 1 = day, 2 = month, 3 = year, 4 = custom."),
            },
            GridHGT = GridEnums.HGTGridEnum.DatosGeneralesContrato,
            GridTypeHGT = GriTypeEnums.HGTGridTypeEnum.Abastecimiento
        };
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }

    [HttpGet("purchase/order/audit")]
    [EndpointSummary("Auditoría órdenes de compra.")]
    [EndpointDescription("Representa la grilla Auditoría órdenes de compra")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoiceReceiptsEcuadorAsync(
        [FromQuery]
        [Description("Tipo de filtro: 1 = dia anterior, 2 = Mes actual, 3 = Año anterior o últimos 12 meses")]
        ApiRequestEnum typeFilter,
        CancellationToken cancellationToken,
        [FromQuery]
        [Description("Número de página, se inicia con 1")]
        int page = 1,
        [FromQuery]
        [Description("Número de registros a obtener.")]
        int? pagSize = null)
    {
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.AuditoriaOC);
        var query = new GridDataOnlyGetQuery
        {
            Username = User.Identity.Name,
            Password = User.Claims.FirstOrDefault(i => i.Type == "Password")?.Value,
            Organization = User.Claims.FirstOrDefault(i => i.Type == "Organization")?.Value,
            FunctionName = gridSettings.UserFunction,
            GridName = gridSettings.GridName,
            GridId = gridSettings.GridId,
            Page = page,
            NumberOfRowsFirstReturned = !pagSize.HasValue ? gridSettings.NumberRecordsFirstReturned : pagSize.GetValueOrDefault(),
            DataspyId = typeFilter switch
            {
                ApiRequestEnum.Day => gridSettings.DataSpyIds.Day,
                ApiRequestEnum.Month => gridSettings.DataSpyIds.Month,
                ApiRequestEnum.Year => gridSettings.DataSpyIds.Year,
                ApiRequestEnum.Custom => gridSettings.DataSpyIds.Custom,
                _ => throw new InvalidOperationException("Invalid filter, accepted values ​​are: 1 = day, 2 = month, 3 = year, 4 = custom."),
            },
            GridHGT = GridEnums.HGTGridEnum.AuditoriaOC,
            GridTypeHGT = GriTypeEnums.HGTGridTypeEnum.Abastecimiento
        };
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }

    [HttpGet("view/purchase/order")]
    [EndpointSummary("Vista finanzas órdenes de compra (OC).")]
    [EndpointDescription("Representa la grilla Vista finanzas OC")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchaseOrderAsync(
        [FromQuery]
        [Description("Tipo de filtro: 1 = dia anterior, 2 = Mes actual, 3 = Año anterior o últimos 12 meses")]
        ApiRequestEnum typeFilter,
        CancellationToken cancellationToken,
        [FromQuery]
        [Description("Número de página, se inicia con 1")]
        int page = 1,
        [FromQuery]
        [Description("Número de registros a obtener.")]
        int? pagSize = null)
    {
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.VistaFinanzasOC);
        var query = new GridDataOnlyGetQuery
        {
            Username = User.Identity.Name,
            Password = User.Claims.FirstOrDefault(i => i.Type == "Password")?.Value,
            Organization = User.Claims.FirstOrDefault(i => i.Type == "Organization")?.Value,
            FunctionName = gridSettings.UserFunction,
            GridName = gridSettings.GridName,
            GridId = gridSettings.GridId,
            Page = page,
            NumberOfRowsFirstReturned = !pagSize.HasValue ? gridSettings.NumberRecordsFirstReturned : pagSize.GetValueOrDefault(),
            DataspyId = typeFilter switch
            {
                ApiRequestEnum.Day => gridSettings.DataSpyIds.Day,
                ApiRequestEnum.Month => gridSettings.DataSpyIds.Month,
                ApiRequestEnum.Year => gridSettings.DataSpyIds.Year,
                ApiRequestEnum.Custom => gridSettings.DataSpyIds.Custom,
                _ => throw new InvalidOperationException("Invalid filter, accepted values ​​are: 1 = day, 2 = month, 3 = year, 4 = custom."),
            },
            GridHGT = GridEnums.HGTGridEnum.VistaFinanzasOC,
            GridTypeHGT = GriTypeEnums.HGTGridTypeEnum.Abastecimiento
        };
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }

    [HttpGet("view/purchase/request")]
    [EndpointSummary("Vista finanzas solicitud de compra (SC).")]
    [EndpointDescription("Representa la grilla Vista finanzas SC")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchaseRequestAsync(
    [FromQuery]
        [Description("Tipo de filtro: 1 = dia anterior, 2 = Mes actual, 3 = Año anterior o últimos 12 meses")]
        ApiRequestEnum typeFilter,
        CancellationToken cancellationToken,
        [FromQuery]
        [Description("Número de página, se inicia con 1")]
        int page = 1,
        [FromQuery]
        [Description("Número de registros a obtener.")]
        int? pagSize = null)
    {
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.VistaFinanzasSC);
        var query = new GridDataOnlyGetQuery
        {
            Username = User.Identity.Name,
            Password = User.Claims.FirstOrDefault(i => i.Type == "Password")?.Value,
            Organization = User.Claims.FirstOrDefault(i => i.Type == "Organization")?.Value,
            FunctionName = gridSettings.UserFunction,
            GridName = gridSettings.GridName,
            GridId = gridSettings.GridId,
            Page = page,
            NumberOfRowsFirstReturned = !pagSize.HasValue ? gridSettings.NumberRecordsFirstReturned : pagSize.GetValueOrDefault(),
            DataspyId = typeFilter switch
            {
                ApiRequestEnum.Day => gridSettings.DataSpyIds.Day,
                ApiRequestEnum.Month => gridSettings.DataSpyIds.Month,
                ApiRequestEnum.Year => gridSettings.DataSpyIds.Year,
                ApiRequestEnum.Custom => gridSettings.DataSpyIds.Custom,
                _ => throw new InvalidOperationException("Invalid filter, accepted values ​​are: 1 = day, 2 = month, 3 = year, 4 = custom."),
            },
            GridHGT = GridEnums.HGTGridEnum.VistaFinanzasSC,
            GridTypeHGT = GriTypeEnums.HGTGridTypeEnum.Abastecimiento
        };
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }
}
