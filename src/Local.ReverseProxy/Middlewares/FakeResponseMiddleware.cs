using Local.ReverseProxy.Services;

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

        public async Task InvokeAsync(HttpContext context)
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
            var logMessage = $"Processing {context.Request.Method} {context.Request.Path}{context.Request.QueryString}\n" +
                             string.Join("\n", headers) +
                             (headers.Count > 0 ? "\n" : "") +
                             payload;

            _logger.LogInformation(logMessage);

            context.Response.StatusCode = 200;
            context.Response.ContentType = "application/json";

            // Write headers and payload to response
            var responseObj = new
            {
                message = "OK"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(responseObj);
            await context.Response.WriteAsync(json);
        }
    }
}
