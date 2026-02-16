using HGT.EAM.WebServices.Application.Models;
using HGT.EAM.WebServices.Application.Queries;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using HGT.EAM.WebServices.Infrastructure.Architecture.Controller;
using HGT.EAM.WebServices.Infrastructure.Architecture.Enums;
using HGT.EAM.WebServices.Infrastructure.Architecture.Models;
using Mediator;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Net;

namespace HGT.EAM.WebServices.Application.Controllers;

public abstract class BaseGridController : HGTController
{
    private readonly IEnumerable<EAMGridSettings> _gridSettings;

    protected BaseGridController(
        IMediator mediator,
        ILogger logger,
        IEnumerable<EAMGridSettings> gridSettings)
        : base(mediator, logger)
    {
        _gridSettings = gridSettings;
    }

    protected async Task<IActionResult> ExecuteGridQuery(
        GridEnums.HGTGridEnum gridName,
        GridTypeEnums.HGTGridTypeEnum gridType,
        GridRequestParams request,
        CancellationToken cancellationToken)
    {
        var settings = _gridSettings.FirstOrDefault(f => f.HGTGridName == gridName);
        if (settings == null)
            return NotFound($"Configuration for grid {gridName} not found.");

        var query = new GridDataOnlyGetQuery(
            User,
            request.TypeFilter,
            settings,
            gridName,
            gridType,
            request.Page,
            request.PageSize,
            request.Month,
            request.Year);

        return await ExecuteHandler<GridDataOnlyGetQuery, ResultDataGridModel>(query, HttpStatusCode.OK, cancellationToken);
    }
}
