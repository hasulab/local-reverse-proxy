using System.Collections.ObjectModel;

namespace Local.ReverseProxy.Services
{
    public class HttpFileRoute
    {
        public string FileName { get; set; }
        public string Method { get; set; }
        public string Url { get; set; }
        public bool UrlValid { get; set; }
        public string UrlHost { get; set; }
        public string UrlPath { get; set; }
        public string Body { get; internal set; }
        public IReadOnlyDictionary<string,string> Headers { get; internal set; } = Defaults.EmptyString2Dictionary;
        public int StatusCode { get; set; }
        public IReadOnlyList<HttpFileUrlSegment> UrlSegments { get; internal set; }
        public string QueryString { get; internal set; }
        public IReadOnlyDictionary<string, HttpFileUrlSegment> QuerySegments { get; internal set; } = Defaults.EmptyStringHttpFileUrlSegmentDictionary;
        
        public override string ToString()
        {
            return $"{Method} {Url} {StatusCode}\n" +
                   $"{string.Join("\n", Headers.Select(h => $"{h.Key}: {h.Value}"))}\n" +
                   $"{Body}";
        }
    }
    public record HttpFileUrlSegment(string Segment, bool HasVariable, string VariableName = null);
}