namespace Local.ReverseProxy.Services
{
    public class HttpFileInfo
    {
        public string FileName { get; set; }
        public string Method { get; set; }
        public string Url { get; set; }
        public string Body { get; internal set; }
        public Dictionary<string,string> Headers { get; internal set; }
    }
}