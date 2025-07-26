using System.Diagnostics.CodeAnalysis;

namespace Local.ReverseProxy.Services
{
    public class FileService : IFileService
    {
        public string Combine(string path1, string path2)
        {
            return Path.Combine(path1, path2);
        }

        public bool DirectoryExists([NotNullWhen(true)] string? path)
        {
            return !string.IsNullOrEmpty(path) && Directory.Exists(path);
        }

        public bool FileExists([NotNullWhen(true)] string? path)
        {
            return !string.IsNullOrEmpty(path) && File.Exists(path);
        }

        public string? GetFileName(string? path)
        {
            return string.IsNullOrEmpty(path) ? null : Path.GetFileName(path);
        }

        public string[] GetFiles(string path, string searchPattern)
        {
            return Directory.Exists(path) ? Directory.GetFiles(path, searchPattern) : Array.Empty<string>();
        }

        public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
        {
            return File.ReadAllTextAsync(path, cancellationToken);
        }
    }

}
