
using System.Diagnostics.CodeAnalysis;

namespace Local.ReverseProxy.Services
{
    public interface IHttpFileService
    {
        IEnumerable<HttpFileRoute> GetHttpFilesInfo();
        Task<IEnumerable<HttpFileRoute>> ParseHttpFile(string file);
        bool Exists([NotNullWhen(true)] string? path);
        bool ValidateUrl(HttpRequest request, out HttpFileRoute matchedRoute, out IReadOnlyDictionary<string, string> outParams);
    }
    public interface IFileService
    {
        bool FileExists([NotNullWhen(true)] string? path);
        bool DirectoryExists([NotNullWhen(true)] string? path);
        string? GetFileName(string? path);
        Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
        string Combine(string path1, string path2);
        string[] GetFiles(string path, string searchPattern);
    }
}