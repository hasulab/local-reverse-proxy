using Local.ReverseProxy.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

namespace Local.ReverseProxy.Services
{
    public class TokenValidateService : ITokenValidateService
    {
        private readonly AuthenticationConfig _authenticationConfig;
        private readonly ILogger<TokenValidateService> _logger;
        private static SecurityKey _cachedSigningKey;
        private static DateTime _lastKeyFetch = DateTime.MinValue;

        public TokenValidateService(AuthenticationConfig authenticationConfig, ILogger<TokenValidateService> logger)
        {
            _authenticationConfig = authenticationConfig;
            _logger = logger;
        }
        public async Task<(bool isValid, ClaimsPrincipal? principal)> ValidateTokenAsync(HttpContext context)
        {
            var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("Token is missing");
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
                    ValidIssuer = _authenticationConfig.AzureAd.Issuer,
                    ValidAudience = _authenticationConfig.AzureAd.Audience,
                    ValidAudiences = _authenticationConfig.AzureAd.Audiences,
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

            var jwksUrl = $"https://login.microsoftonline.com/{_authenticationConfig.AzureAd.TenantId}/discovery/v2.0/keys";
            using var httpClient = new HttpClient();
            var jwksJson = await httpClient.GetStringAsync(jwksUrl);
            var jwks = JsonSerializer.Deserialize<JwksResponse>(jwksJson);

            if (jwks?.keys == null || jwks.keys.Length == 0)
            {
                throw new Exception("No signing keys found.");
            }

            var key = jwks.keys[0]; // Assume the first key is used for signing
            var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = Base64UrlDecode(key.n),
                Exponent = Base64UrlDecode(key.e)
            });

            _cachedSigningKey = new RsaSecurityKey(rsa);
            _lastKeyFetch = DateTime.UtcNow;

            return _cachedSigningKey;
        }

        private static byte[] Base64UrlDecodeOLD(string input)
        {
            string padded = input.PadRight(input.Length + (4 - input.Length % 4) % 4, '=');
            return Convert.FromBase64String(padded.Replace('-', '+').Replace('_', '/'));
        }

        private static byte[] Base64UrlDecode(string input)
        {
            string base64 = input.Replace('-', '+').Replace('_', '/');
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }

        public class TokenHeader
        {
            public string typ { get; set; }
            public string nonce { get; set; }
            public string alg { get; set; }
            public string x5t { get; set; }
            public string kid { get; set; }
        }

        public class TokenPayload
        {
            public string aud { get; set; }
            public string iss { get; set; }
            public int iat { get; set; }
            public int nbf { get; set; }
            public int exp { get; set; }
            public int acct { get; set; }
            public string acr { get; set; }
            public string aio { get; set; }
            public string[] amr { get; set; }
            public string app_displayname { get; set; }
            public string appid { get; set; }
            public string appidacr { get; set; }
            public string idtyp { get; set; }
            public string ipaddr { get; set; }
            public string name { get; set; }
            public string oid { get; set; }
            public string platf { get; set; }
            public string puid { get; set; }
            public string rh { get; set; }
            public string scp { get; set; }
            public string sid { get; set; }
            public string sub { get; set; }
            public string tenant_region_scope { get; set; }
            public string tid { get; set; }
            public string unique_name { get; set; }
            public string upn { get; set; }
            public string uti { get; set; }
            public string ver { get; set; }
            public string[] wids { get; set; }
            public string xms_idrel { get; set; }
            public int xms_tcdt { get; set; }
        }

        public class JwksResponse
        {
            public JwkKey[] keys { get; set; }
        }

        public class JwkKey
        {
            public string kty { get; set; }
            public string use { get; set; }
            public string kid { get; set; }
            public string x5t { get; set; }
            public string n { get; set; } // Modulus
            public string e { get; set; } // Exponent
            public string[] x5c { get; set; }
            public string cloud_instance_name { get; set; }
            public string issuer { get; set; }
        }

    }

    public interface ITokenValidateService
    {
        Task<(bool isValid, ClaimsPrincipal? principal)> ValidateTokenAsync(HttpContext context);
    }
}
