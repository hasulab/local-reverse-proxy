
using System.Diagnostics.CodeAnalysis;

namespace Local.ReverseProxy.Services
{
    public interface IHttpFileService
    {
        IEnumerable<HttpFileInfo> GetHttpFilesInfo();
        Task<IEnumerable<HttpFileInfo>> ParseHttpFile(string file);
        bool Exists([NotNullWhen(true)] string? path);
    }
}