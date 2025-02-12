namespace Local.ReverseProxy.Models
{
    public class AuthenticationConfig
    {
        public AzureAd AzureAd { get; set; }
        public List<string> AuthExlusions { get; set; }
    }
}
