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
        _gridSettings = gridSettings.FindAll(filter => filter.HGTGridType == Infrastructure.Architecture.Enums.GriTypeEnums.HGTGridTypeEnum.CuentasPorPagar);
    }

    [HttpGet("invoice/vouchers/ecuador")]
    [EndpointSummary("Lista de comprobantes de factura de Ecuador.")]
    [EndpointDescription("Representa la grilla comprobantes de factura Ecuador")]
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
        int? pagSize = null))
    {
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.ListaComprobantesFacturaEcuador);
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
            GridHGT = GridEnums.HGTGridEnum.ListaComprobantesFacturaEcuador,
            GridTypeHGT = GriTypeEnums.HGTGridTypeEnum.CuentasPorPagar
        };
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }

    [HttpGet("billing/finance/view")]
    [EndpointSummary("Vista finanzas facturación.")]
    [EndpointDescription("Representa la grilla vista finanzas facturación")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBillingFinanceViewAsync(
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
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.VistaFinanzasFacturacion);
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
            GridHGT = GridEnums.HGTGridEnum.VistaFinanzasFacturacion,
            GridTypeHGT = GriTypeEnums.HGTGridTypeEnum.CuentasPorPagar
        };
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }

    [HttpGet("billing/finance/order-purchase")]
    [EndpointSummary("Vista finanzas OC.")]
    [EndpointDescription("Representa la grilla vista finanzas de órdenes de compra")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBillingFinanceOCAsync(
        [FromQuery]
        [Description("Tipo de filtro: 1 = dia anterior, 2 = Mes actual, 3 = Año anterior o últimos 12 meses")]
        ApiRequestEnum typeFilter,
        CancellationToken cancellationToken,
        [FromQuery]
        [Description("Número de página, se inicia con 1")]
        int page = 1,
        [FromQuery]
        [Description("Número de registros a obtener.")]
        int? pagSize = null))
    {
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.VistaOrdenesDeCompras);
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
            GridTypeHGT = GriTypeEnums.HGTGridTypeEnum.CuentasPorPagar
        };
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }
}
