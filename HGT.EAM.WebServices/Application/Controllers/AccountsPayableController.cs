using EAM.WebServices;
using HGT.EAM.WebServices.Application.Queries;
using HGT.EAM.WebServices.Infrastructure.Architecture.Controller;
using HGT.EAM.WebServices.Infrastructure.Architecture.Enums;
using HGT.EAM.WebServices.Infrastructure.Architecture.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using static HGT.EAM.WebServices.Infrastructure.Architecture.Enums.ApiFilterEnums;

namespace HGT.EAM.WebServices.Application.Controllers;

[Authorize]
[Tags("Cuentas por pagar")]
[ApiController]
[Route("api/accounts-payable")]
public class AccountsPayableController : HGTController
{
    private readonly List<EAMGridSettings> _gridSettings;

    public AccountsPayableController(
        IMediator mediator, 
        ILogger<TestController> logger,
        List<EAMGridSettings> gridSettings
        )
        : base(mediator, logger)
    {
        _gridSettings = gridSettings.FindAll(filter => filter.HGTGridType == Infrastructure.Architecture.Enums.GriTypeEnums.HGTGridTypeEnum.CuentasPorPagar);
    }

    /// <summary>Lista de comprobantes de factura de Ecuador.</summary>
    /// <param name="typeFilter">Filtro para e</param>
    [HttpGet("invoice/vouchers/ecuador")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInvoiceReceiptsEcuadorAsync(
        [FromQuery] ApiRequestEnum typeFilter, CancellationToken cancellationToken)
    {
        var gridInvoiceVocherSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == Infrastructure.Architecture.Enums.GridEnums.HGTGridEnum.ListaComprobantesFacturaEcuador);
        var query = new GridDataOnlyGetQuery
        {
            Username = User.Identity.Name,
            Password = User.Claims.FirstOrDefault(i => i.Type == "Password")?.Value,
            Organization = User.Claims.FirstOrDefault(i => i.Type == "Organization")?.Value,
            FunctionName = gridInvoiceVocherSettings.UserFunction,
            GridName = gridInvoiceVocherSettings.GridName,
            GridId = gridInvoiceVocherSettings.GridId,
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
        return await ExecuteHandler<GridDataOnlyGetQuery, MP0116_GetGridDataOnly_001_Result>(query, HttpStatusCode.OK, cancellationToken);
    }
}
