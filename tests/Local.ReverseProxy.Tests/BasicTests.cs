using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;

namespace Local.ReverseProxy.Tests
{
    public class BasicTests
: IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public BasicTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            //EndpointRouteBuilderExtensions.IsE2ETestCall = true;
        }

        [Theory]
        [InlineData("/api/events/")]
        public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsync(url, JsonContent.Create("{}"));

            // Assert
            response.EnsureSuccessStatusCode(); // Status Code 200-299
        }
    }
}