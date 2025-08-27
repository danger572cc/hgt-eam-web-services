using EAM.WebServices;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using Mapster;
using Microsoft.AspNetCore.Mvc.ViewEngines;

namespace HGT.EAM.WebServices.Application.Mapper;

public static class MapsterConfig
{
    public static void Configure()
    {
        TypeAdapterConfig<FIELD, Field>.NewConfig()
            .Map(dest => dest.Id, src => Convert.ToInt32(src.aliasnum))
            .Map(dest => dest.Label, src => src.label)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Order, src => Convert.ToInt32(src.order))
            .Map(dest => dest.Type, src => src.type)
            .Map(dest => dest.Visible, src => src.visible == "+")
            .Map(dest => dest.Width, src => src.width);
    }
}