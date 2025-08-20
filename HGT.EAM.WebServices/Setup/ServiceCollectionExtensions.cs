namespace HGT.EAM.WebServices.Setup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        /*services.AddScoped<ISmsService, SmsService>();
        services.AddSingleton<IEncryptionService, EncryptionService>();
        services.AddScoped<IDgaApiService, DgaApiService>();
        services.AddScoped<ITempSessionService, TempSessionService>();
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IS3Service, S3Service>();
        services.AddScoped<IBillingStaticDataService, BillingStaticDataService>();*/
        return services;
    }
}