using Local.ReverseProxy.Middlewares;
using Local.ReverseProxy.Transforms;
using Microsoft.AspNetCore.Routing.Matching;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<EndpointSelector, CustomEndpointSelector>();
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
     .AddTransforms(builderContext =>
     {
     //builderContext.Add(new WSToJsonTransformer());
     //https://microsoft.github.io/reverse-proxy/articles/transforms.html         
     });


var app = builder.Build();

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


public class CustomEndpointSelector : EndpointSelector
{
    public override Task SelectAsync(HttpContext httpContext, CandidateSet candidates)
    {
        for (int i = 0; i < candidates.Count; i++)
        {
            var endpoint = candidates[i].Endpoint;
            if (endpoint.DisplayName == "SpecialEndpoint")
            {
                candidates.SetValidity(i, true);
            }
            else
            {
                candidates.SetValidity(i, false);
            }
        }

        return Task.CompletedTask;
    }
}