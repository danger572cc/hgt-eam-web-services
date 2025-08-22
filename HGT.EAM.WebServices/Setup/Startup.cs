using HGT.EAM.WebServices.Infrastructure.Architecture.Extensions;
using HGT.EAM.WebServices.Infrastructure.Architecture.Middlewares;
using Microsoft.Extensions.Options;
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
        app.MapOpenApi();
        app.MapScalarApiReference();
        //app.UseHttpsRedirection();
        app.MapControllers();
        app.UseMiddleware<ExceptionMiddleware>()
            .UseMiddleware<ResponseMiddleware>();
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
                builder.WithOrigins("https://octocelio.cl", "https://www.octocelio.cl")
                       .SetIsOriginAllowed(origin => origin.EndsWith(".octocelio.cl"))
                       .AllowAnyMethod()
                       .AllowAnyHeader();
            });*/
        });
        services.AddControllers();
        //services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());
        services.AddApplicationServices(configuration);
        services.AddConfigOpenApi(configuration);
        /*services.AddMediator(cfg =>
        {
            cfg.Assemblies = [Assembly.GetExecutingAssembly()];
        });*/
        services.AddMediator((Mediator.MediatorOptions options) =>
        {
            options.ServiceLifetime = ServiceLifetime.Singleton;
            options.GenerateTypesAsInternal = true;
            options.Assemblies = [Assembly.GetExecutingAssembly()];
        });
        services.AddMemoryCache();
    }
}