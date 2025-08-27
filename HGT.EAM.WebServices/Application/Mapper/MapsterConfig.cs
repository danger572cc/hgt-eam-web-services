using EAM.WebServices;
using HGT.EAM.WebServices.Conector.Architecture.Models;
using Mapster;

namespace HGT.EAM.WebServices.Application.Mapper;

public static class MapsterConfig
{
    public static void Configure()
    {
        TypeAdapterConfig<FIELD, Field>.NewConfig()
            .Map(dest => dest.Id, src => Convert.ToInt32(src.aliasnum))
            .Map(dest => dest.Label, src => src.label)
            .Map(dest => dest.Name, src => src.name)
            .Map(dest => dest.Order, src => !string.IsNullOrEmpty(src.order) ? Convert.ToInt32(src.order) : 0)
            .Map(dest => dest.Type, src => src.type)
            .Map(dest => dest.Visible, src => !string.IsNullOrEmpty(src.visible) && src.visible == "+")
            .Map(dest => dest.Width, src => !string.IsNullOrEmpty(src.width) ? Convert.ToInt32(src.width) : 0);
    }
}