using HGT.EAM.WebServices.Infrastructure.Architecture.Extensions;
using HGT.EAM.WebServices.Setup;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
builder.AddBasicAuthorization();
builder.Host.UseSerilog((context, configuration) =>
{
    var options = new Serilog.Settings.Configuration.ConfigurationReaderOptions(
        typeof(Serilog.ConsoleLoggerConfigurationExtensions).Assembly,
        typeof(Serilog.FileLoggerConfigurationExtensions).Assembly,
        typeof(Serilog.LoggerConfigurationMSSqlServerExtensions).Assembly
    );
    configuration
        .ReadFrom.Configuration(context.Configuration, options)
        .Enrich.FromLogContext();
});
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services, builder.Configuration);
var app = builder.Build();
startup.Configure(app);
app.Run();