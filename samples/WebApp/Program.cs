using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Tokens;
using WebApp;

var builder = WebApplication.CreateBuilder(args);

var tenantId = builder.Configuration["Authentication:AzureAd:TenantId"];
var clientId = builder.Configuration["Authentication:AzureAd:ClientId"];

//https://stackoverflow.com/questions/74196824/idx10511-signature-validation-failed-keys-tried-microsoft-identitymodel-toke
//https://github.com/AzureAD/azure-activedirectory-identitymodel-extensions-for-dotnet/issues/1334

//olny for web
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration, configSectionName: "Authentication:AzureAd");

//only for console/webapis
/*
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var config = builder.Services.BuildServiceProvider().GetService<AuthenticationConfig>();

        options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
        options.Audience = $"api://{clientId}";// "https://graph.microsoft.com"; // $"{clientId}";
        options.RequireHttpsMetadata = true;
        options.IncludeErrorDetails = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://sts.windows.net/{tenantId}/", // $"https://login.microsoftonline.com/{tenantId}/v2.0",
            ValidateAudience = true,
            ValidAudience = $"api://{clientId}",
            ValidateLifetime = true,
            RequireSignedTokens = true, // true,
            ValidateIssuerSigningKey = true //true
        };
    });
*/

//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//   .AddMicrosoftIdentityWebApi(options =>
//       {
//           var config = builder.Services.BuildServiceProvider().GetService<AuthenticationConfig>();

//           options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
//           options.Audience = $"api://{clientId}";// "https://graph.microsoft.com"; // $"{clientId}";
//           options.RequireHttpsMetadata = true;
//           options.IncludeErrorDetails = true;
//           options.TokenValidationParameters = new TokenValidationParameters
//           {
//               ValidateIssuer = true,
//               ValidIssuer = $"https://sts.windows.net/{tenantId}/", // $"https://login.microsoftonline.com/{tenantId}/v2.0",
//               ValidateAudience = true,
//               ValidAudience = $"api://{clientId}",
//               ValidateLifetime = true,
//               RequireSignedTokens = true, // true,
//               ValidateIssuerSigningKey = true //true
//           };
//       },
//       msiOption =>
//       {
//           msiOption.ClientId = clientId;
//       });

builder.Services.Configure<AuthenticationConfig>(builder.Configuration.GetSection("Authentication"))
    .AddSingleton(ConfigurationBinder.Get<AuthenticationConfig>(builder.Configuration.GetSection("Authentication")))
    .AddAuthorization()
    .AddEndpointsApiExplorer()
    .AddSwaggerGen();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var config = scope.ServiceProvider.GetService<AuthenticationConfig>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.UseMiddleware<MyAuthenticationMiddleware>();

app.Use( async (context, next) =>
{
    var endpoint = context.GetEndpoint();
    // Do work that can write to the Response.
    await next.Invoke();
    // Do logging or other work that doesn't write to the Response.
});
app.Use([Authorize] async (context, next) =>
{
    var endpoint = context.GetEndpoint();
    // Do work that can write to the Response.
    await next.Invoke();
    // Do logging or other work that doesn't write to the Response.
    var authFeatures1 = context.Features.Get<IHttpAuthenticationFeature>();
    var authFeatures2 = context.Features.Get<IAuthenticateResultFeature>();
});


app.UseAuthentication();
app.UseAuthorization();


app.MapGet("/weatherforecast", [Authorize] (HttpContext context) =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();

app.Use(async (context, next) =>
{
    var endpoint = context.GetEndpoint();
    // Do work that can write to the Response.
    await next.Invoke();
    // Do logging or other work that doesn't write to the Response.
});


app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
