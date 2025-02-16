using Microsoft.Extensions.Configuration;

namespace Local.ReverseProxy.Tests
{
    public class TestBase
    {
        public TestBase()
        {

        }

        public IConfigurationRoot GetConfiguration()
        {
            return new ConfigurationBuilder()
                .AddJsonFile("./appsettings.json")
                .AddUserSecrets<TestBase>()
                .Build();
        }
    }
}