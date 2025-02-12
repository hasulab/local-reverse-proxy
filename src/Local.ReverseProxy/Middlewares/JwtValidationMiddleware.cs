using Local.ReverseProxy.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text.Json;

namespace Local.ReverseProxy.Middlewares
{
    public class JwtValidationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly AuthenticationConfig _authentication;
        private static SecurityKey _cachedSigningKey;
        private static DateTime _lastKeyFetch = DateTime.MinValue;

        public JwtValidationMiddleware(RequestDelegate next, 
            IConfiguration configuration, AuthenticationConfig authentication)
        {
            _next = next;
            _configuration = configuration;
            _authentication = authentication;
        }

        public async Task Invoke(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if (_authentication.AuthExlusions.Contains(endpoint.DisplayName))
            {
                await _next(context);
                return;
            }

            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var validationResult = await ValidateTokenAsync(token);
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

        private async Task<(bool isValid, System.Security.Claims.ClaimsPrincipal? principal)> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return (false, null);
            }

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                // Fetch JWKS key dynamically
                var signingKey = await GetSigningKeyAsync();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["Jwt:Issuer"],
                    ValidAudience = _configuration["Jwt:Audience"],
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
                return (true, principal);
            }
            catch
            {
                return (false, null);
            }
        }

        private async Task<SecurityKey> GetSigningKeyAsync()
        {
            // Cache key for a limited time to avoid frequent API calls
            if (_cachedSigningKey != null && DateTime.UtcNow - _lastKeyFetch < TimeSpan.FromHours(12))
            {
                return _cachedSigningKey;
            }

            var jwksUrl = _configuration["Jwt:JwksUrl"]; // Example: https://your-idp.com/.well-known/jwks.json
            using var httpClient = new HttpClient();
            var jwksJson = await httpClient.GetStringAsync(jwksUrl);
            var jwks = JsonSerializer.Deserialize<JwksResponse>(jwksJson);

            if (jwks?.Keys == null || jwks.Keys.Length == 0)
            {
                throw new Exception("No signing keys found.");
            }

            var key = jwks.Keys[0]; // Assume the first key is used for signing
            var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = Base64UrlDecode(key.N),
                Exponent = Base64UrlDecode(key.E)
            });

            _cachedSigningKey = new RsaSecurityKey(rsa);
            _lastKeyFetch = DateTime.UtcNow;

            return _cachedSigningKey;
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string padded = input.PadRight(input.Length + (4 - input.Length % 4) % 4, '=');
            return Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
        }

        private class JwksResponse
        {
            public JwkKey[] Keys { get; set; }
        }

        private class JwkKey
        {
            public string Kty { get; set; }
            public string Use { get; set; }
            public string Kid { get; set; }
            public string N { get; set; }  // Modulus
            public string E { get; set; }  // Exponent
        }
    }

}
