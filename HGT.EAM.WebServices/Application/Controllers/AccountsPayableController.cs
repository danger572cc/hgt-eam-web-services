using EAM.WebServices;
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
    [EndpointDescription("Representa la grilla Comprobantes de factura Ecuador")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetInvoiceReceiptsEcuadorAsync(
        [FromQuery]
        [Description("Tipo de filtro: 1 = dias, 2 = Mes, 3 = Año")]
        ApiRequestEnum typeFilter,
        CancellationToken cancellationToken,
        [FromQuery]
        [Description("Número de página, se inicia con 0")]
        int page = 1)
    {
        var gridInvoiceVocherSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.ListaComprobantesFacturaEcuador);
        var query = new GridDataOnlyGetQuery
        {
            Username = User.Identity.Name,
            Password = User.Claims.FirstOrDefault(i => i.Type == "Password")?.Value,
            Organization = User.Claims.FirstOrDefault(i => i.Type == "Organization")?.Value,
            FunctionName = gridInvoiceVocherSettings.UserFunction,
            GridName = gridInvoiceVocherSettings.GridName,
            GridId = gridInvoiceVocherSettings.GridId,
            Page = page,
            NumberOfRowsFirstReturned = gridInvoiceVocherSettings.NumberRecordsFirstReturned,
            DataspyId = typeFilter switch
            {
                ApiRequestEnum.Day => gridInvoiceVocherSettings.DataSpyIds.Day,
                ApiRequestEnum.Month => gridInvoiceVocherSettings.DataSpyIds.Month,
                ApiRequestEnum.Year => gridInvoiceVocherSettings.DataSpyIds.Year,
                _ => throw new InvalidOperationException("Invalid filter, accepted values ​​are: 1 = day, 2 = month, 3 = year."),
            },
            GridHGT = GridEnums.HGTGridEnum.ListaComprobantesFacturaEcuador,
            GridTypeHGT = GriTypeEnums.HGTGridTypeEnum.CuentasPorPagar
        };
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }
}
