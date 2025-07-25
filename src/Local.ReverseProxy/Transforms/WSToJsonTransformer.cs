using System.Text;
using Yarp.ReverseProxy.Forwarder;

namespace Local.ReverseProxy.Transforms
{
    public class WSToJsonTransformer : HttpTransformer
    {
        public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix, CancellationToken cancellationToken)
        {
            // Call the base method to copy the headers and other default settings
            await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

            // Log request headers
            foreach (var header in httpContext.Request.Headers)
            {
                Console.WriteLine($"Request Header: {header.Key} = {header.Value}");
            }

            // Log and modify request body if it's a POST or PUT request
            if (httpContext.Request.Method == HttpMethods.Post || httpContext.Request.Method == HttpMethods.Put)
            {
                httpContext.Request.EnableBuffering(); // Enable buffering to read the body multiple times
                using var reader = new StreamReader(httpContext.Request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                Console.WriteLine($"Request Body: {body}");
                httpContext.Request.Body.Position = 0; // Reset the position for further reading

                // Optionally modify the request body
                var newBody = body + " Additional content";
                proxyRequest.Content = new StringContent(newBody, Encoding.UTF8, httpContext.Request.ContentType);
            }
        }
       
        public override async ValueTask<bool> TransformResponseAsync(HttpContext httpContext, HttpResponseMessage? proxyResponse, CancellationToken cancellationToken)
        {
            // Log response headers
            foreach (var header in proxyResponse.Headers)
            {
                Console.WriteLine($"Response Header: {header.Key} = {string.Join(", ", header.Value)}");
            }

            // Log response body
            if (proxyResponse.Content != null)
            {
                var responseBody = await proxyResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"Response Body: {responseBody}");
            }

            return await base.TransformResponseAsync(httpContext, proxyResponse, cancellationToken);
        }
    }
}
