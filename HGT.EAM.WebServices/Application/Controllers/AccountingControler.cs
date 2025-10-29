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
[Tags("Contabilidad")]
[ApiController]
[Route("api/accounting")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[ResponseCache(Duration = 900)]
public class AccountingControler : HGTController
{
    private readonly List<EAMGridSettings> _gridSettings;

    public AccountingControler(
        IMediator mediator,
        ILogger<AccountingControler> logger,
        List<EAMGridSettings> gridSettings
        )
        : base(mediator, logger)
    {
        _gridSettings = gridSettings.FindAll(filter => filter.HGTGridType == GriTypeEnums.HGTGridTypeEnum.Contabilidad);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("transactions")]
    [EndpointSummary("Grilla de transacciones.")]
    [EndpointDescription("Representa la grilla transacciones")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactionsAsync(
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
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.GrillaTransacciones);
        var query = new GridDataOnlyGetQuery(User, typeFilter, gridSettings, GridEnums.HGTGridEnum.GrillaTransacciones, GriTypeEnums.HGTGridTypeEnum.Contabilidad, page, pagSize, month, year);
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }

    [ResponseCache(Duration = 900)]
    [HttpGet("kardex")]
    [EndpointSummary("Informe de Kardex.")]
    [EndpointDescription("Representa al informe de cognos informe de kardex")]
    [ProducesResponseType(typeof(ResultDataGridModel), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKardexAsync(
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
        var gridSettings = _gridSettings.FirstOrDefault(f => f.HGTGridName == GridEnums.HGTGridEnum.Kardex);
        var query = new GridDataOnlyGetQuery(User, typeFilter, gridSettings, GridEnums.HGTGridEnum.Kardex, GriTypeEnums.HGTGridTypeEnum.Contabilidad, page, pagSize, month, year);
        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }
}
