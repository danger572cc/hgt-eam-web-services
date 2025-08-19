using HGT.EAM.WebServices.Infraestructure.Architecture.Extensions;
using HGT.EAM.WebServices.Infraestructure.Architecture.Middlewares;
using Scalar.AspNetCore;
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
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapOpenApi();
        app.MapScalarApiReference();
        //app.UseHttpsRedirection();
        app.MapControllers();
        app.UseMiddleware<ExceptionMiddleware>()
            .UseMiddleware<ResponseMiddleware>();
    }

    public void ConfigureServices(IServiceCollection services)
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
        services.AddConfigOpenApi();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });
        services.AddMemoryCache();
    }
}