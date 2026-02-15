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
        _gridSettings = gridSettings.FindAll(filter => filter.HGTGridType == GridTypeEnums.HGTGridTypeEnum.Abastecimiento);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("contracts")]
    [EndpointSummary("Datos generales del contrato.")]
    [EndpointDescription("Representa la grilla Datos generales del contrato")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInfoContractsAsync(
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
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.DatosGeneralesContrato);
        var query = new GridDataOnlyGetQuery(User, typeFilter, gridSettings, GridEnums.HGTGridEnum.DatosGeneralesContrato, GridTypeEnums.HGTGridTypeEnum.Abastecimiento, page, pagSize, month, year);
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("purchase/order/audit")]
    [EndpointSummary("Auditoría órdenes de compra.")]
    [EndpointDescription("Representa la grilla Auditoría órdenes de compra")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoiceReceiptsEcuadorAsync(
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
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.AuditoriaOC);
        var query = new GridDataOnlyGetQuery(User, typeFilter, gridSettings, GridEnums.HGTGridEnum.AuditoriaOC, GridTypeEnums.HGTGridTypeEnum.Abastecimiento, page, pagSize, month, year);
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("view/purchase/order")]
    [EndpointSummary("Vista finanzas órdenes de compra (OC).")]
    [EndpointDescription("Representa la grilla Vista finanzas OC")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchaseOrderAsync(
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
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.VistaFinanzasOC);
        var query = new GridDataOnlyGetQuery(User, typeFilter, gridSettings, GridEnums.HGTGridEnum.VistaFinanzasOC, GridTypeEnums.HGTGridTypeEnum.Abastecimiento, page, pagSize, month, year);
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("view/purchase/request")]
    [EndpointSummary("Vista finanzas solicitud de compra (SC).")]
    [EndpointDescription("Representa la grilla Vista finanzas SC")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchaseRequestAsync(
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
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.VistaFinanzasSC);
        var query = new GridDataOnlyGetQuery(User, typeFilter, gridSettings, GridEnums.HGTGridEnum.VistaFinanzasSC, GridTypeEnums.HGTGridTypeEnum.Abastecimiento, page, pagSize, month, year);
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }
}
