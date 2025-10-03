using HGT.EAM.WebServices.Application.Mapper;
using HGT.EAM.WebServices.Infrastructure.Architecture.Extensions;
using HGT.EAM.WebServices.Infrastructure.Architecture.Middlewares;
using Mapster;
using Scalar.AspNetCore;
using Serilog;
using System.Reflection;

namespace HGT.EAM.WebServices.Setup;

public class Startup(IConfiguration configuration)
{
    private readonly IConfiguration _configuration = configuration;

    public void Configure(WebApplication app)
    {
        if (app.Environment.IsDevelopment())
        {
            app.UseCors("AllowAll");
            app.UseExceptionHandler("/error");
        }
        /*else
        {
            app.UseCors("HGT");
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }*/
        app.UseSerilogRequestLogging();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseStaticFiles();
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.Title = "HGT Grid API - EAM";
            options.Layout = ScalarLayout.Modern;
            options.ShowSidebar = true;
            options.DarkMode = false;
            options.HideDarkModeToggle = false;
            options.DefaultOpenAllTags = true;
            options.Favicon = "/images/favicon.ico";
            options.HeadContent = @"<div style='position:fixed;top:0;left:0;width:100%;height:4.5%;z-index:100;color:white;background-color: #222A36 !important;'><a href='https://www.aep.cl/'><img src='https://www.aep.cl/wp-content/uploads/2025/07/logoHGT-blanco.png' alt='Hanseatic Global Terminals' style='width: 7%;margin-top: 5px;'></a></div>";
        });
        //app.UseHttpsRedirection();
        app.MapControllers();
        app.UseMiddleware<ExceptionMiddleware>()
            .UseMiddleware<ResponseMiddleware>()
            .UseMiddleware<QueryParamsValidationMiddleware>();
        app.UseResponseCaching();
    }

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", builder =>
            {
                builder.AllowAnyOrigin()
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });
            /*options.AddPolicy("HGT", builder =>
            {
                builder.WithOrigins("https://eamdev.hgtlatam.com/")
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });*/
        });
        services.AddControllers();
        services.AddApplicationServices(configuration);
        services.AddConfigOpenApi(configuration);
        services.AddMapster();
        MapsterConfig.Configure();
        services.AddMediator((Mediator.MediatorOptions options) =>
        {
            options.ServiceLifetime = ServiceLifetime.Singleton;
            options.GenerateTypesAsInternal = true;
            options.Assemblies = [Assembly.GetExecutingAssembly()];
        });
        services.AddMemoryCache();
        services.AddResponseCaching();
    }
}