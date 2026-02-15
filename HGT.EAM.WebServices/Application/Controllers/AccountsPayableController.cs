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
[Tags("Cuentas por pagar")]
[ApiController]
[Route("api/accounts-payable")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class AccountsPayableController : HGTController
{
    private readonly List<EAMGridSettings> _gridSettings;

    public AccountsPayableController(
        IMediator mediator,
        ILogger<AccountsPayableController> logger,
        List<EAMGridSettings> gridSettings
        )
        : base(mediator, logger)
    {
        _gridSettings = gridSettings.FindAll(filter => filter.HGTGridType == Infrastructure.Architecture.Enums.GridTypeEnums.HGTGridTypeEnum.CuentasPorPagar);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("invoice/vouchers/ecuador")]
    [EndpointSummary("Lista de comprobantes de factura de Ecuador.")]
    [EndpointDescription("Representa la grilla comprobantes de factura Ecuador")]
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
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.ListaComprobantesFacturaEcuador);
        var query = new GridDataOnlyGetQuery(User, typeFilter, gridSettings, GridEnums.HGTGridEnum.ListaComprobantesFacturaEcuador, GridTypeEnums.HGTGridTypeEnum.CuentasPorPagar, page, pagSize, month, year);
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("billing/finance/view")]
    [EndpointSummary("Vista finanzas facturación.")]
    [EndpointDescription("Representa la grilla vista finanzas facturación")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBillingFinanceViewAsync(
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
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.VistaFinanzasFacturacion);
        var query = new GridDataOnlyGetQuery(User, typeFilter, gridSettings, GridEnums.HGTGridEnum.VistaFinanzasFacturacion, GridTypeEnums.HGTGridTypeEnum.CuentasPorPagar, page, pagSize, month, year);
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("billing/finance/order-purchase")]
    [EndpointSummary("Vista finanzas OC.")]
    [EndpointDescription("Representa la grilla vista finanzas de órdenes de compra")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBillingFinanceOCAsync(
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
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.VistaOrdenesDeCompras);
        var query = new GridDataOnlyGetQuery(User, typeFilter, gridSettings, GridEnums.HGTGridEnum.VistaFinanzasOC, GridTypeEnums.HGTGridTypeEnum.CuentasPorPagar, page, pagSize, month, year);
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }
}
