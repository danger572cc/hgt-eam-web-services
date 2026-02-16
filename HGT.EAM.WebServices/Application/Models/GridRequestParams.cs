using HGT.EAM.WebServices.Infrastructure.Architecture.Enums;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace HGT.EAM.WebServices.Application.Models;

public class GridRequestParams
{
    [FromQuery]
    [Description("Tipo de filtro: 1 = dia anterior, 2 = Mes anterior, 3 = Mes actual, 4 = Año anterior, 5 = Mes y año en concreto")]
    public ApiFilterEnums.ApiRequestEnum TypeFilter { get; set; }

    [FromQuery]
    [Description("Mes en concreto a buscar, el rango de valores válidos es: 1-12")]
    public int? Month { get; set; }

    [FromQuery]
    [Description("Año en concreto, valores validos a partir del año anterior")]
    public int? Year { get; set; }

    [FromQuery]
    [Description("Número de página, se inicia con 1")]
    public int Page { get; set; } = 1;

    [FromQuery]
    [Description("Número de registros a obtener.")]
    public int? PageSize { get; set; }
}
