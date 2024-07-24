using Local.ReverseProxy.Middlewares;
using Local.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
     .AddTransforms(builderContext =>
     {
     //builderContext.Add(new WSToJsonTransformer());
     //https://microsoft.github.io/reverse-proxy/articles/transforms.html         
     });


var app = builder.Build();

// Add request-response logging middleware
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.MapGet("/", () => "Hello World!");
app.MapReverseProxy();
app.Run();
