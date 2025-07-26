namespace Local.ReverseProxy.Services
{
    public class HttpFileInfo
    {
        public string FileName { get; set; }
        public string Method { get; set; }
        public string Url { get; set; }
        public bool UrlValid { get; set; }
        public string UrlHost { get; set; }
        public string UrlPath { get; set; }
        public string Body { get; internal set; }
        public Dictionary<string,string> Headers { get; internal set; }
        public int StatusCode { get; set; }
        public string[] UrlSegments { get; internal set; }
    }
}