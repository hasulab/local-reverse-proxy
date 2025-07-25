using System.Text.RegularExpressions;

namespace Local.ReverseProxy.Middlewares
{
    public class HttpFileMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _basePath;

        public HttpFileMiddleware(RequestDelegate next, string basePath)
        {
            _next = next;
            _basePath = basePath; // e.g., "C:\\HttpFiles" or "/var/www/httpfiles"
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Check if the request path matches a pattern for .http files
            // For simplicity, let's assume a direct mapping: /api/foo -> foo.http
            // You might want more sophisticated routing.
            var filePath = Path.Combine(_basePath, context.Request.Path.Value.TrimStart('/') + ".http");

            if (File.Exists(filePath))
            {
                try
                {
                    var fileContent = await File.ReadAllTextAsync(filePath);
                    var (statusCode, headers, body) = ParseHttpFile(fileContent);

                    context.Response.StatusCode = statusCode;

                    foreach (var header in headers)
                    {
                        context.Response.Headers[header.Key] = header.Value;
                    }

                    if (!string.IsNullOrEmpty(body))
                    {
                        await context.Response.WriteAsync(body);
                    }

                    return; // Short-circuit the pipeline if a file is served
                }
                catch (Exception ex)
                {
                    // Log the error and optionally return an error response
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync($"Error processing .http file: {ex.Message}");
                    return;
                }
            }

            // If no matching .http file, continue to the next middleware in the pipeline
            await _next(context);
        }

        private (int statusCode, Dictionary<string, string> headers, string body) ParseHttpFile(string fileContent)
        {
            int statusCode = StatusCodes.Status200OK; // Default status code
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            string body = null;

            using (StringReader reader = new StringReader(fileContent))
            {
                string line;
                bool inBody = false;

                while ((line = reader.ReadLine()) != null)
                {
                    if (!inBody)
                    {
                        // Check for HTTP status line (e.g., HTTP/1.1 200 OK)
                        var statusMatch = Regex.Match(line, @"^HTTP/\d\.\d\s+(\d+)\s*.*");
                        if (statusMatch.Success)
                        {
                            if (int.TryParse(statusMatch.Groups[1].Value, out int parsedStatusCode))
                            {
                                statusCode = parsedStatusCode;
                            }
                            continue;
                        }

                        // Check for empty line separating headers from body
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            inBody = true;
                            continue;
                        }

                        // Parse headers (e.g., Content-Type: application/json)
                        var headerMatch = Regex.Match(line, @"^([\w-]+):\s*(.*)");
                        if (headerMatch.Success)
                        {
                            headers[headerMatch.Groups[1].Value] = headerMatch.Groups[2].Value.Trim();
                        }
                    }
                    else
                    {
                        // Accumulate body content
                        if (body == null)
                        {
                            body = line;
                        }
                        else
                        {
                            body += Environment.NewLine + line;
                        }
                    }
                }
            }

            return (statusCode, headers, body);
        }
    }

}