using Microsoft.AspNetCore.Routing.Matching;

namespace Local.ReverseProxy.Transforms
{
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

}
