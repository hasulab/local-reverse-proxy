using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Local.ReverseProxy.Tests
{
    public class JwtTokenTest : TestBase
    {
        [Fact]
        public void Test1()
        {
            var configuration = GetConfiguration();
            var tenantId = configuration["Authentication:AzureAd:TenantId"];
            var clientId = configuration["Authentication:AzureAd:ClientId"];

            var jwtToken = File.ReadAllText("D:\\token1.txt");
            jwtToken = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(jwtToken));
            var parts = jwtToken.Split('.');
            var header = JsonSerializer.Deserialize<TokenHeader>(Base64UrlDecode(parts[0]));
            var payload = JsonSerializer.Deserialize<TokenPayload>(Base64UrlDecode(parts[1]));

            var jwksJson = File.ReadAllText("D:\\jwksKeys.json");
            var jwks = JsonSerializer.Deserialize<JwksResponse>(jwksJson);

            var key = jwks.keys.First(k => k.kid == header.kid);

            var rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = Base64UrlDecode(key.n),
                Exponent = Base64UrlDecode(key.e)
            });

            var _cachedSigningKey = new RsaSecurityKey(rsa);

            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                // Fetch JWKS key dynamically
                var signingKey = _cachedSigningKey;// await GetSigningKeyAsync();

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = $"https://sts.windows.net/{tenantId}/", //_configuration["Jwt:Issuer"],
                    ValidAudience = $"api://{clientId}",
                    IssuerSigningKey = signingKey,
                    ClockSkew = TimeSpan.Zero
                };

                var principal = tokenHandler.ValidateToken(jwtToken, validationParameters, out _);
                //return (true, principal);
            }
            catch(Exception ex)
            {
                ;
                //return (false, null);
            }
        }

        private static byte[] Base64UrlDecode1(string input)
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
}