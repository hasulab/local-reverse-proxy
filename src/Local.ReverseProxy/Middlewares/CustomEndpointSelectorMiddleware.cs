namespace Local.ReverseProxy.Middlewares
{
    /// <summary>
    /// this sample is for swagger through reverse proxy
    /// </summary>
    public class CustomEndpointSelectorMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly EndpointDataSource _endpointDataSource;

        public CustomEndpointSelectorMiddleware(RequestDelegate next, EndpointDataSource endpointDataSource)
        {
            _next = next;
            _endpointDataSource = endpointDataSource;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var endpoint = context.GetEndpoint();
            if ((endpoint == null || endpoint.DisplayName == "AllRoute") 
                && !string.IsNullOrEmpty(context.Request.Headers["Referer"].ToString()) )
            {
                var endpoints = _endpointDataSource.Endpoints;
                var refererUrlSegments = context.Request.Headers["Referer"].ToString().Split('/');
                // If the referer URL has more than 3 segments, then it is a swagger URL
                if (refererUrlSegments.Length > 3)
                {
                    // Select the matching first endpoint from the list
                    //write your custom logic here
                    var pathPrefix = refererUrlSegments[3] ?? string.Empty;
                    var selectedEndpoint = endpoints
                        .FirstOrDefault(e => e.DisplayName == pathPrefix 
                                            || (e.Metadata.Count > 1 && e.Metadata.Any(x=> x.ToString().Contains($"/{pathPrefix}/"))));
                    //endpoints[1].Metadata[1] as Microsoft.AspNetCore.Routing.RouteEndpointBuilder.RouteDiagnosticsMetadata
                    if (selectedEndpoint != null)
                    {
                        context.SetEndpoint(selectedEndpoint);
                    }
                }                
            }

            await _next(context);
        }
    }
}
