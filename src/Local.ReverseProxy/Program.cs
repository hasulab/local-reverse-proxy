using Local.ReverseProxy.Extensions;
using Local.ReverseProxy.Middlewares;
using Local.ReverseProxy.Models;
using Local.ReverseProxy.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);
//builder.Services.AddSingleton<EndpointSelector, CustomEndpointSelector>();
builder.Services.Configure<AuthenticationConfig>(builder.Configuration.GetSection("Authentication"));
//builder.Environment.EnvironmentName = "Development";
builder.Services
    .AddTransient<ITokenValidateService, TokenValidateService>()
    .AddSingleton(ConfigurationBinder.Get<AuthenticationConfig>(builder.Configuration.GetSection("Authentication")));

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
     .AddTransforms(builderContext =>
     {
     //builderContext.Add(new WSToJsonTransformer());
     //https://microsoft.github.io/reverse-proxy/articles/transforms.html         
     });
var tenantId = builder.Configuration["Authentication:AzureAd:TenantId"];
var clientId = builder.Configuration["Authentication:AzureAd:ClientId"];

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.Audience = $"{clientId}"; // Your Azure AD Application ID

        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidIssuer = $"https://login.microsoftonline.com/{tenantId}/v2.0",
            ValidAudience = $"{clientId}"
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpFile();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();


app.UseHttpFileMiddleware(builder.Configuration["HttpFilesBasePath"] ?? ".\\HttpFiles");
app.UseProxyMiddleware(); 
    
app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint();
    // Do work that can write to the Response.
    await next.Invoke();
    // Do logging or other work that doesn't write to the Response.
});

// Add request-response logging middleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.MapGet("/", () => "Hello World!");
app.MapReverseProxy();
app.Run();

public partial class Program { }