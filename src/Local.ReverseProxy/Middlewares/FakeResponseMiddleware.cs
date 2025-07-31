using Local.ReverseProxy.Services;
using Microsoft.AspNetCore.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace Local.ReverseProxy.Middlewares
{
    public class FakeResponseMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IHttpFileService _httpFileService;
        private readonly ILogger<FakeResponseMiddleware> _logger;

        public FakeResponseMiddleware(RequestDelegate next,
                                      IHttpFileService httpFileService,
                                      ILogger<FakeResponseMiddleware> logger)
        {
            _next = next;
            _httpFileService = httpFileService;
            _logger = logger;
        }

        static readonly Regex placeholderRegex = new Regex(@"\{([a-zA-Z0-9_]+)\}");
        public async Task InvokeAsync(HttpContext context)
        {
            if (_httpFileService.ValidateUrl(context.Request, out HttpFileRoute matchedRoute, out var outParams))
            {
                try
                {
                    // Read request headers
                    var headers = context.Request.Headers
                        .Select(h => $"{h.Key}: {string.Join(", ", h.Value.ToArray())}")
                        .ToList();

                    // Read request body (payload)
                    string payload = string.Empty;
                    bool isChunked = context.Request.Headers.TryGetValue("Transfer-Encoding", out var te)
                                     && te.ToString().ToLower().Contains("chunked");

                    if ((context.Request.ContentLength > 0 || isChunked) && context.Request.Body.CanRead)
                    {
                        context.Request.EnableBuffering();
                        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                        payload = await reader.ReadToEndAsync();
                        context.Request.Body.Position = 0;
                    }

                    // Log info: method, url, headers (each on new line), payload
                    var logMessage = $"Processing {context.Request.Method} {context.Request.Path} {DateTime.Now:G}\n" +
                        $"{context.Request.Method} {context.Request.Path}{context.Request.QueryString}\n" +
                                     string.Join("\n", headers) +
                                     (headers.Count > 0 ? "\n" : "") +
                                     payload +
                                     "###";

                    _logger.LogInformation(logMessage);

                    // Write headers and payload to response
                    context.Response.StatusCode = matchedRoute.StatusCode;
                    context.Response.ContentType = "application/json";
                    foreach (var header in matchedRoute.Headers)
                    {
                        context.Response.Headers[header.Key] = header.Value;
                    }
                    if (!string.IsNullOrEmpty(matchedRoute.Body))
                    {
                        var bodyText = matchedRoute.Body;
                        if (outParams != null && outParams.Any())
                        {
                            //var sbBody = new StringBuilder();
                            //foreach (var param in outParams){ sbBody.Replace($"{{{{{param.Key}}}}}", param.Value);}
                            //bodyText = sbBody.ToString();
                            bodyText = placeholderRegex.Replace(matchedRoute.Body, match =>
                            {
                                var key = match.Groups[1].Value;
                                return outParams.TryGetValue(key, out var value) ? value : match.Value;
                            });
                        }
                        await context.Response.WriteAsync(bodyText);
                    }
                    
                }
                catch (Exception ex)
                {
                    // Log the error and optionally return an error response
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync($"Error processing fake .http file: {ex.Message}");
                    return;
                }
            }
            else
            {
                // If no matching .http file, continue to the next middleware in the pipeline
                await _next(context);
            }


        }
    }
}
