using Local.ReverseProxy.Services;
using System.Collections.ObjectModel;

namespace Local.ReverseProxy
{
    public static class Defaults
    {
        //public static string DefaultHttpFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "reverseproxy", "httpfiles");
        public static string DefaultHttpFilesPath = "./HttpFiles"; // Default path for .http files, can be overridden in configuration
        public static ReadOnlyDictionary<string, string> EmptyString2Dictionary = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>());
        public static ReadOnlyDictionary<string, HttpFileUrlSegment> EmptyStringHttpFileUrlSegmentDictionary = new ReadOnlyDictionary<string, HttpFileUrlSegment>(new Dictionary<string, HttpFileUrlSegment>());
    }
}
