using HGT.EAM.WebServices.Application.Models;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.Enums;
using HGT.EAM.WebServices.Infrastructure.Architecture.Models;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HGT.EAM.WebServices.Application.Controllers;

[Authorize]
[Tags("Cuentas por pagar")]
[ApiController]
[Route("api/accounts-payable")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class AccountsPayableController : BaseGridController
{
    public AccountsPayableController(
        IMediator mediator,
        ILogger<AccountsPayableController> logger,
        List<EAMGridSettings> gridSettings
        )
        : base(mediator, logger, gridSettings)
    {
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("invoice/vouchers/ecuador")]
    [EndpointSummary("Lista de comprobantes de factura de Ecuador.")]
    [EndpointDescription("Representa la grilla comprobantes de factura Ecuador")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoiceReceiptsEcuadorAsync(
        [FromQuery] GridRequestParams request,
        CancellationToken cancellationToken)
    {
        return await ExecuteGridQuery(
            GridEnums.HGTGridEnum.ListaComprobantesFacturaEcuador,
            GridTypeEnums.HGTGridTypeEnum.CuentasPorPagar,
            request,
            cancellationToken);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("billing/finance/view")]
    [EndpointSummary("Vista finanzas facturación.")]
    [EndpointDescription("Representa la grilla vista finanzas facturación")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBillingFinanceViewAsync(
        [FromQuery] GridRequestParams request,
        CancellationToken cancellationToken)
    {
        return await ExecuteGridQuery(
            GridEnums.HGTGridEnum.VistaFinanzasFacturacion,
            GridTypeEnums.HGTGridTypeEnum.CuentasPorPagar,
            request,
            cancellationToken);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("billing/finance/order-purchase")]
    [EndpointSummary("Vista finanzas OC.")]
    [EndpointDescription("Representa la grilla vista finanzas de órdenes de compra")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBillingFinanceOCAsync(
        [FromQuery] GridRequestParams request,
        CancellationToken cancellationToken)
    {
        return await ExecuteGridQuery(
            GridEnums.HGTGridEnum.VistaFinanzasOC,
            GridTypeEnums.HGTGridTypeEnum.CuentasPorPagar,
            request,
            cancellationToken);
    }
}
