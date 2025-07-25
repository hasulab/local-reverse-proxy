using Local.ReverseProxy.Middlewares;

namespace Local.ReverseProxy.Extensions
{
    public static class MiddlewareExtensions
    {
        public static IApplicationBuilder UseHttpFileMiddleware(this IApplicationBuilder builder, string basePath)
        {
            return builder.UseMiddleware<HttpFileMiddleware>(basePath);
            return builder.UseMiddleware<FakeResponseMiddleware>(basePath);

        }
        public static IApplicationBuilder UseProxyMiddleware(this IApplicationBuilder builder)
        {
            builder.UseMiddleware<CustomEndpointSelectorMiddleware>();
            //app.UseMiddleware<JwtValidationMiddleware>();
            return builder;
        }

    }
}
