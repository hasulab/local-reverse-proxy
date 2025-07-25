namespace Local.ReverseProxy.Services
{
    public class HttpFileService : IHttpFileService
    {
        private readonly string _folderPath;
        private List<HttpFileInfo>? _cache;
        private bool _cacheInitialized = false;
        private readonly object _cacheLock = new();

        public HttpFileService(string? folderPath = null)
        {
            _folderPath = folderPath ?? Path.Combine(Directory.GetCurrentDirectory(), "HttpFiles");
        }

        public IEnumerable<HttpFileInfo> GetHttpFilesInfo()
        {
            if (_cacheInitialized && _cache != null)
                return _cache;

            lock (_cacheLock)
            {
                if (_cacheInitialized && _cache != null)
                    return _cache;

                if (!Directory.Exists(_folderPath))
                    _cache = new List<HttpFileInfo>();
                else
                {
                    var files = Directory.GetFiles(_folderPath, "*.http");
                    var result = new List<HttpFileInfo>();
                    foreach (var file in files)
                    {
                        var lines = File.ReadLines(file)
                            .Where(l => !string.IsNullOrWhiteSpace(l) && !l.TrimStart().StartsWith("#"))
                            .ToList();
                        if (lines.Count == 0)
                            continue;

                        var firstLine = lines[0].Trim();
                        var parts = firstLine.Split(' ', 2);
                        if (parts.Length != 2)
                            continue;

                        var info = new HttpFileInfo
                        {
                            FileName = Path.GetFileName(file),
                            Method = parts[0],
                            Url = parts[1]
                        };

                        // Parse headers and body
                        int i = 1;
                        for (; i < lines.Count; i++)
                        {
                            var line = lines[i];
                            if (string.IsNullOrWhiteSpace(line))
                            {
                                i++; // Move to the line after the blank line
                                break;
                            }

                            if (!line.Contains(':'))
                                break;

                            var headerParts = line.Split(':', 2);
                            if (headerParts.Length == 2)
                                info.Headers[headerParts[0].Trim()] = headerParts[1].Trim();
                        }

                        // The rest is body
                        if (i < lines.Count)
                            info.Body = string.Join("\n", lines.Skip(i));

                        result.Add(info);
                    }

                    _cache = result;
                }

                _cacheInitialized = true;
                return _cache;
            }
        }

    }
}
