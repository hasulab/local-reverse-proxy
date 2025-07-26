using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Local.ReverseProxy.Services
{
    public class HttpFileService : IHttpFileService
    {
        private readonly IFileService _fileService;
        private readonly string _folderPath;
        private List<HttpFileInfo>? _cache;
        private bool _cacheInitialized = false;
        private readonly object _cacheLock = new();

        public HttpFileService(IFileService fileService, string? folderPath = null)
        {
            _fileService = fileService;
            _folderPath = folderPath ?? _fileService.Combine(Directory.GetCurrentDirectory(), "HttpFiles");
        }
        public IEnumerable<HttpFileInfo> GetHttpFilesInfo()
        {
            if (_cacheInitialized && _cache != null)
                return _cache;

            lock (_cacheLock)
            {
                if (_cacheInitialized && _cache != null)
                    return _cache;

                if (!_fileService.DirectoryExists(_folderPath))
                    _cache = new List<HttpFileInfo>();
                else
                {
                    var files = _fileService.GetFiles(_folderPath, "*.http");
                    var result = new List<HttpFileInfo>();
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

        public async Task<IEnumerable<HttpFileInfo>> ParseHttpFile(string file)
        {
            var listFileInfo = new List<HttpFileInfo>();

            var fullFileContent = await _fileService.ReadAllTextAsync(file);
            var fileContentParts = fullFileContent.Split("###");
            var fileName = _fileService.GetFileName(file);
            foreach (var fileContent in fileContentParts)
            {
                var httpFile = ParseHttpFileContent(fileContent);
                httpFile.FileName = fileName;
                listFileInfo.Add(httpFile);
            }
            return listFileInfo;
        }

        private HttpFileInfo ParseHttpFileContent(string fileContent)
        {
            var info = new HttpFileInfo
            {
                Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
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
                            info.Method = firstLineMatch.Groups[1].Value.Trim();
                            info.Url = firstLineMatch.Groups[2].Value.Trim();
                        }
                        else
                        {
                            throw new FormatException("Invalid HTTP request line format.");
                        }
                        isFirstLine = false;
                    } 
                    else if (!inBody)
                    {
                        // Check for HTTP status line (e.g., HTTP/1.1 200 OK)
                        var statusMatch = Regex.Match(line, @"^HTTP/\d\.\d\s+(\d+)\s*.*");
                        if (statusMatch.Success)
                        {
                            if (int.TryParse(statusMatch.Groups[1].Value, out int parsedStatusCode))
                            {
                                info.StatusCode = parsedStatusCode;
                            }
                            continue;
                        }

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
                            info.Headers[headerMatch.Groups[1].Value] = headerMatch.Groups[2].Value.Trim();
                        }
                    }
                    else
                    {
                        // Accumulate body content
                        if (string.IsNullOrEmpty(info.Body))
                        {
                            info.Body = line;
                        }
                        else
                        {
                            info.Body += Environment.NewLine + line;
                        }
                    }
                }
            }
            return info;
        }

        public bool Exists([NotNullWhen(true)] string? path)
        {
            return _fileService.FileExists(path) || _fileService.DirectoryExists(path);
        }
    }

}
