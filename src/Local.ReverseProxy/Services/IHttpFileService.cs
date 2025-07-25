
namespace Local.ReverseProxy.Services
{
    public interface IHttpFileService
    {
        IEnumerable<HttpFileInfo> GetHttpFilesInfo();
    }
}