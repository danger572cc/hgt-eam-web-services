using HGT.EAM.WebServices.Application.Models;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.Enums;
using HGT.EAM.WebServices.Infrastructure.Architecture.Models;
using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using static HGT.EAM.WebServices.Infrastructure.Architecture.Enums.GridTypeEnums;

namespace HGT.EAM.WebServices.Application.Controllers;

[Authorize]
[Tags("Abastecimiento")]
[ApiController]
[Route("api/provisions")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
public class ProvisionController : BaseGridController
{
    public ProvisionController(
        IMediator mediator,
        ILogger<ProvisionController> logger,
        List<EAMGridSettings> gridSettings
        )
        : base(mediator, logger, gridSettings)
    {
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("contracts")]
    [EndpointSummary("Datos generales del contrato.")]
    [EndpointDescription("Representa la grilla Datos generales del contrato")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInfoContractsAsync(
        [FromQuery] GridRequestParams request,
        CancellationToken cancellationToken)
    {
        return await ExecuteGridQuery(
            GridEnums.HGTGridEnum.DatosGeneralesContrato,
            HGTGridTypeEnum.Abastecimiento,
            request,
            cancellationToken);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("purchase/order/audit")]
    [EndpointSummary("Auditoría órdenes de compra.")]
    [EndpointDescription("Representa la grilla Auditoría órdenes de compra")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchaseOrderAuditAsync(
        [FromQuery] GridRequestParams request,
        CancellationToken cancellationToken)
    {
        return await ExecuteGridQuery(
            GridEnums.HGTGridEnum.AuditoriaOC,
            HGTGridTypeEnum.Abastecimiento,
            request,
            cancellationToken);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("view/purchase/order")]
    [EndpointSummary("Vista finanzas órdenes de compra (OC).")]
    [EndpointDescription("Representa la grilla Vista finanzas OC")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchaseOrderAsync(
        [FromQuery] GridRequestParams request,
        CancellationToken cancellationToken)
    {
        return await ExecuteGridQuery(
            GridEnums.HGTGridEnum.VistaFinanzasOC,
            HGTGridTypeEnum.Abastecimiento,
            request,
            cancellationToken);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("view/purchase/request")]
    [EndpointSummary("Vista finanzas solicitud de compra (SC).")]
    [EndpointDescription("Representa la grilla Vista finanzas SC")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPurchaseRequestAsync(
        [FromQuery] GridRequestParams request,
        CancellationToken cancellationToken)
    {
        return await ExecuteGridQuery(
            GridEnums.HGTGridEnum.VistaFinanzasSC,
            HGTGridTypeEnum.Abastecimiento,
            request,
            cancellationToken);
    }
}
