using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;

namespace Local.ReverseProxy.Services
{
    public class HttpFileService : IHttpFileService
    {
        private readonly IFileService _fileService;
        private readonly string _folderPath;
        private List<HttpFileRoute>? _cache;
        private bool _cacheInitialized = false;
        private readonly object _cacheLock = new();

        public HttpFileService(IFileService fileService, string? folderPath = null)
        {
            _fileService = fileService;
            _folderPath = folderPath ?? _fileService.Combine(Directory.GetCurrentDirectory(), "HttpFiles");
        }
        public IEnumerable<HttpFileRoute> GetHttpFilesInfo()
        {
            if (_cacheInitialized && _cache != null)
                return _cache;

            lock (_cacheLock)
            {
                if (_cacheInitialized && _cache != null)
                    return _cache;

                if (!_fileService.DirectoryExists(_folderPath))
                    _cache = new List<HttpFileRoute>();
                else
                {
                    var files = _fileService.GetFiles(_folderPath, "*.http");
                    var result = new List<HttpFileRoute>();
                    foreach (var file in files)
                    {
                        var fileIfoList = ParseHttpFile(file).GetAwaiter().GetResult(); // Synchronously wait for the async method
                        result.AddRange(fileIfoList);
                    }

                    _cache = result;
                }

                _cacheInitialized = true;
                return _cache;
            }
        }

        public async Task<IEnumerable<HttpFileRoute>> ParseHttpFile(string file)
        {
            var routes = new List<HttpFileRoute>();

            var fullFileContent = await _fileService.ReadAllTextAsync(file);
            var fileContentParts = fullFileContent.Split("###");
            var fileName = _fileService.GetFileName(file);
            foreach (var fileContent in fileContentParts)
            {
                var httpFileRoute = ParseHttpFileContent(fileContent);
                httpFileRoute.FileName = fileName;
                routes.Add(httpFileRoute);
            }
            return routes;
        }

        private HttpFileRoute ParseHttpFileContent(string fileContent)
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var route = new HttpFileRoute
            {
                Headers = headers,
                Body = string.Empty,
                StatusCode = StatusCodes.Status200OK
            };

            if (string.IsNullOrWhiteSpace(fileContent))
            {
                throw new ArgumentException("File content cannot be null or empty.", nameof(fileContent));
            }

            // Trim leading whitespace to handle any indentation
            fileContent = fileContent.TrimStart();

            using (StringReader reader = new StringReader(fileContent))
            {
                string line;
                bool inBody = false;
                bool isFirstLine = true;

                while ((line = reader.ReadLine()) != null)
                {
                    if (line.TrimStart().StartsWith("#"))
                    {
                        continue;// Skip comments
                    }
                    if (isFirstLine)
                    {
                        // Parse the first line for method and URL (e.g., GET /api/resource)
                        var firstLineMatch = Regex.Match(line, @"^(\w+)\s+(\S+)");
                        if (firstLineMatch.Success)
                        {
                            route.Method = firstLineMatch.Groups[1].Value.Trim();
                            route.Url = firstLineMatch.Groups[2].Value.Trim();
                            var parsedUrl = ValidateUrlInternal(route.Url);
                            if (!parsedUrl.isValid)
                            {
                                throw new FormatException($"Invalid URL format: {route.Url}");
                            }
                            route.UrlHost = parsedUrl.host;
                            route.UrlPath = parsedUrl.path;
                            route.UrlValid = parsedUrl.isValid;
                            route.QueryString = parsedUrl.query;
                            route.UrlSegments = BuldUrlSegments(parsedUrl.path);
                            if (!string.IsNullOrEmpty(route.QueryString))
                            {
                                route.QuerySegments = route.QueryString.Split('&')
                                    .Select(q => q.Split('='))
                                    .ToDictionary(kv => kv[0], kv => kv.Length > 1 ? kv[1] : string.Empty, StringComparer.OrdinalIgnoreCase);
                            }
                        }
                        else
                        {
                            throw new FormatException("Invalid HTTP request line format.");
                        }
                        isFirstLine = false;
                    } 
                    else if (!inBody)
                    {
                        // Check for empty line separating headers from body
                        if (string.IsNullOrWhiteSpace(line))
                        {
                            inBody = true;
                            continue;
                        }

                        // Parse headers (e.g., Content-Type: application/json)
                        var headerMatch = Regex.Match(line, @"^([\w-]+):\s*(.*)");
                        if (headerMatch.Success)
                        {
                            var headerName = headerMatch.Groups[1].Value;
                            var headerValue = headerMatch.Groups[2].Value.Trim();
                            headers[headerName] = headerValue;
                            if (headerName.Equals("Status-Code", StringComparison.OrdinalIgnoreCase))
                            {
                                // Check for HTTP status line (e.g., HTTP/1.1 200 OK)
                                var statusMatch = Regex.Match(headerValue, @"^HTTP/\d\.\d\s+(\d+)\s*.*");
                                if (statusMatch.Success)
                                {
                                    if (int.TryParse(statusMatch.Groups[1].Value, out int parsedStatusCode))
                                    {
                                        route.StatusCode = parsedStatusCode;
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Accumulate body content
                        if (string.IsNullOrEmpty(route.Body))
                        {
                            route.Body = line;
                        }
                        else
                        {
                            route.Body += Environment.NewLine + line;
                        }
                    }
                }
            }
            return route;
        }

        static Regex UrlRegex = new Regex(@"^(?:https?:\/\/)?(?<host>{{[a-zA-Z0-9_]+}}|[a-zA-Z0-9.-]+(?::\d+)?)(?<path>\/[^\s]*)?$");
        (bool isValid, string host ,string path, string query) ValidateUrlInternal(string? url)
        {
            if (string.IsNullOrEmpty(url))
                return (false, null, null, null);

            if (url.StartsWith("/"))
            {
                ExtractPathAndQuery(url, out string path, out string query);
                return (true, null, path, query);
            }
            else
            {
                var match = UrlRegex.Match(url);
                if (match.Success)
                {
                    var host = match.Groups["host"].Value;
                    var pathNquery = match.Groups["path"].Value;
                    ExtractPathAndQuery(pathNquery, out string path, out string query);

                    return (true, host, path, query);
                }
            }
            return (false, null, null, null);

            static void ExtractPathAndQuery(string url, out string path, out string query)
            {
                var uriParts = url.Split(new[] { '?' }, 2);
                path = uriParts[0];
                query = uriParts.Length > 1 ? '?'+ uriParts[1] : string.Empty;
            }
        }

        IReadOnlyList<HttpFileUrlSegment> BuldUrlSegments(string url)
        {
            var segments = url.Split('/')
                .Select(segment =>
                {
                    bool hasVariable = segment.StartsWith("{{") && segment.EndsWith("}}");
                    var variableName = hasVariable ? segment[2..^2] : null; // Remove the {{ and }} if it's a variable
                    return new HttpFileUrlSegment(segment, hasVariable, variableName);
                })
                .ToList();
            return segments;
        }

        public bool Exists([NotNullWhen(true)] string? path)
        {
            return _fileService.FileExists(path) || _fileService.DirectoryExists(path);
        }

        public bool ValidateUrl(HttpRequest request, out HttpFileRoute matchedRoute,  out IReadOnlyDictionary<string, string> outParams)
        {
            outParams = Defaults.EmptyString2Dictionary;// new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            matchedRoute = null;
            if (request == null || string.IsNullOrEmpty(request.Path))
                return false;

            var pathAndQuery = request.Path.Value+ request.QueryString.Value;
            var path = request.Path.Value;
            var queryString = request.QueryString.Value;
            if (string.IsNullOrEmpty(path))
                return false;

            foreach(var httpFileRoute in _cache ?? GetHttpFilesInfo())
            {
                if (httpFileRoute.Method != request.Method)
                    continue;

                if (httpFileRoute.Url == pathAndQuery
                    || (httpFileRoute.UrlPath == path && httpFileRoute.QueryString == queryString))
                {
                     matchedRoute = httpFileRoute;
                    return true;
                }

                if (MatchPath(httpFileRoute, path, out var pathVariables) && MatchQueryString(httpFileRoute, queryString))
                {
                    matchedRoute = httpFileRoute;
                    return true;
                }
            }
            return false;
        }

        private static bool MatchPath(HttpFileRoute httpFileRoute, string path, out List<string> paramValues)
        {
            paramValues = null;
            if (httpFileRoute.UrlPath == path)
                return true;

            var urlSegments = path.Split('/');
            var cachedSegments = httpFileRoute.UrlSegments;

            if (cachedSegments.Count == urlSegments.Length)
            {
                bool isValid = true;
                for (int i = 0; i < cachedSegments.Count; i++)
                {
                    if (cachedSegments[i].Segment != urlSegments[i] && !cachedSegments[i].HasVariable)
                    {
                        isValid = false;
                        break;
                    }
                    if (cachedSegments[i].HasVariable)
                    {
                        paramValues = paramValues ?? new List<string>();
                        paramValues.Add($"{cachedSegments[i].VariableName}={urlSegments[i]}");
                    }
                }
                if (isValid)
                    return true;
            }
            return false;
        }

        private static bool MatchQueryString(HttpFileRoute httpFileRoute, string queryString)
        {
            if (httpFileRoute.QueryString == queryString)
                return true;

            if (string.IsNullOrEmpty(httpFileRoute.QueryString) == string.IsNullOrEmpty(queryString))
                return true;

            var querySegments = queryString.Split('&')
                .Select(q => q.Split('='))
                .ToDictionary(kv => kv[0], kv => kv.Length > 1 ? kv[1] : string.Empty, StringComparer.OrdinalIgnoreCase);

            if (httpFileRoute?.QuerySegments.Count == querySegments.Count)
            {
                bool isValid = true;
                var cachedQuerySegments = httpFileRoute.QuerySegments.ToArray();
                var querySegmentsArr = querySegments.ToArray();
                for (int i = 0; i < cachedQuerySegments.Length; i++)
                {
                    if (cachedQuerySegments[i].Key != querySegmentsArr[i].Key && !cachedQuerySegments[i].Value.StartsWith("{{"))
                    {
                        isValid = false;
                        break;
                    }
                }
                if (isValid)
                    return true;
            }
            return false;
        }
    }

}
