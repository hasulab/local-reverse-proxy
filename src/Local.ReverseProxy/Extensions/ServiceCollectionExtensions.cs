using Local.ReverseProxy.Services;

namespace Local.ReverseProxy.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddHttpFile(this IServiceCollection services)
        {
            return services.AddSingleton<IHttpFileService, HttpFileService>();
        }
    }
}
