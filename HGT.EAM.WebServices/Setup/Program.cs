using HGT.EAM.WebServices.Infraestructure.Architecture.Extensions;
using HGT.EAM.WebServices.Setup;

var builder = WebApplication.CreateBuilder(args);
builder.AddBasicAuthorization();
var startup = new Startup(builder.Configuration);
startup.ConfigureServices(builder.Services);
var app = builder.Build();
startup.Configure(app);
app.Run();