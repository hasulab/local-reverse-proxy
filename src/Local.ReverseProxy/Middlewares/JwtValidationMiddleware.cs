using Local.ReverseProxy.Models;
using Local.ReverseProxy.Services;

namespace Local.ReverseProxy.Middlewares
{
    public class JwtValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AuthenticationConfig _authentication;
        private readonly ITokenValidateService _tokenValidateService;
        private readonly ILogger<JwtValidationMiddleware> _logger;

        public JwtValidationMiddleware(RequestDelegate next, 
            AuthenticationConfig authentication,
            ITokenValidateService tokenValidateService,
            ILogger<JwtValidationMiddleware> logger)
        {
            _next = next;
            _authentication = authentication;
            _tokenValidateService = tokenValidateService;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (endpoint == null || _authentication.AuthExlusions.Contains(endpoint.DisplayName))
            {
                _logger.LogInformation($"Skipping token validation for {endpoint?.DisplayName}");
                await _next(context);
                return;
            }
            
            var validationResult = await _tokenValidateService.ValidateTokenAsync(context);
            if (validationResult.isValid)
            {
                context.User = validationResult.principal!;
            }
            else
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync($"Invalid or missing token for {endpoint.DisplayName}");
                return;
            }

            await _next(context);
        }

    }

}
