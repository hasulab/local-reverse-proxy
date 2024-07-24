namespace Local.ReverseProxy.Middlewares
{
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        public RequestResponseLoggingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Log request headers
            foreach (var header in context.Request.Headers)
            {
                Console.WriteLine($"Request Header: {header.Key} = {header.Value}");
            }

            // Log request body
            context.Request.EnableBuffering();
            using (var reader = new StreamReader(context.Request.Body, leaveOpen: true))
            {
                var requestBody = await reader.ReadToEndAsync();
                Console.WriteLine($"Request Body: {requestBody}");
                context.Request.Body.Position = 0;
            }

            // Capture and log response
            var originalResponseBodyStream = context.Response.Body;
            using (var responseBodyStream = new MemoryStream())
            {
                context.Response.Body = responseBodyStream;

                await _next(context);

                context.Response.Body.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(context.Response.Body).ReadToEndAsync();
                context.Response.Body.Seek(0, SeekOrigin.Begin);

                Console.WriteLine($"Response Body: {responseBody}");

                await responseBodyStream.CopyToAsync(originalResponseBodyStream);
            }
        }
    }

}
