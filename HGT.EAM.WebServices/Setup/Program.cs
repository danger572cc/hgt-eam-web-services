using HGT.EAM.WebServices.Infrastructure.Architecture.Extensions;
using HGT.EAM.WebServices.Setup;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.AddBasicAuthorization();
builder.Host.UseSerilog((context, configuration) =>
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
);
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services, builder.Configuration);
var app = builder.Build();
startup.Configure(app);
app.Run();