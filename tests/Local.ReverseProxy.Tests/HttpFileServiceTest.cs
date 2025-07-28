using Local.ReverseProxy.Services;
using Microsoft.AspNetCore.Http;
using Moq;

namespace Local.ReverseProxy.Tests
{
    public class HttpFileServiceTest
    {
        [Fact]
        public void DirectoryExistsTest()
        {
            // Arrange
            var moqFileService = new Mock<IFileService>();
            var service = new HttpFileService(moqFileService.Object);
            moqFileService.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
            // Act
            var result = service.Exists("testDirectory");
            // Assert
            Assert.True(result, "Directory should exist");
        }
        [Fact]
        public void FileExistsTest()
        {
            // Arrange
            var moqFileService = new Mock<IFileService>();
            var service = new HttpFileService(moqFileService.Object);
            moqFileService.Setup(fs => fs.FileExists(It.IsAny<string>())).Returns(true);
            // Act
            var result = service.Exists("testFile");
            // Assert
            Assert.True(result, "File should exist");
        }

        [Fact]
        public void ProcessHttpFilesTest()
        {
            // Arrange
            var moqFileService = new Mock<IFileService>();
            var service = new HttpFileService(moqFileService.Object);
            moqFileService.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
            moqFileService
                .Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(["testFile1.http", "testFile2.http"]);
            moqFileService
                .Setup(fs => fs.ReadAllTextAsync("testFile1.http", It.IsAny<CancellationToken>()))
                .ReturnsAsync("GET /test1\n \n{ \"message\":\"OK\" }\n###\nPOST /test2\n \n{ \"message\":\"OK\" }\n");

            moqFileService
                .Setup(fs => fs.ReadAllTextAsync("testFile2.http", It.IsAny<CancellationToken>()))
                .ReturnsAsync("GET /test3\nheader1:val1\n \n{ \"message\":\"OK\" }\n###\nPUT /test4\n \n{ \"message\":\"OK\" }\n");
            moqFileService.Setup(fs => fs.GetFileName(It.IsAny<string>()))
                .Returns((string path) => Path.GetFileName(path));

            // Act
            var result = service.GetHttpFilesInfo();
            // Assert
            Assert.True(result?.Any(), "File should exist");
            Assert.NotNull(result);
            Assert.Equal(4, result.Count());
            Assert.Equal("GET", result.First().Method);
            Assert.Equal("/test1", result.First().Url);
            Assert.Equal("PUT", result.Last().Method);
            Assert.Equal("/test4", result.Last().Url);
        }

        [Theory]
        //[InlineData("GET /test1\n \n{ \"message\":\"OK\" }\n###\nPOST /test2\n \n{ \"message\":\"OK\" }\n", 2)]
        [InlineData("GET", "http://example.com/test1?param=value", true)]
        [InlineData("GET", "http://anyurl.com/test3/1?param=value", true)]
        public void ValidateUrlTest(string method, string url, bool expectedResult)
        {
            string[] urls = {
            "http://anyurl.com/params",
            "https://example.com/api/v1/test",
            "http://{{HOST}}/path/to/resource",
            "http://{{host123}}",
            "http://example.com"
            };
            // Arrange
            var moqFileService = new Mock<IFileService>();
            var service = new HttpFileService(moqFileService.Object);
            moqFileService.Setup(fs => fs.DirectoryExists(It.IsAny<string>())).Returns(true);
            moqFileService
                .Setup(fs => fs.GetFiles(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(["testFile1.http", "testFile2.http"]);

            moqFileService
                .Setup(fs => fs.ReadAllTextAsync("testFile1.http", It.IsAny<CancellationToken>()))
                .ReturnsAsync("GET /test1?param=value\n \n{ \"message\":\"OK\" }\n###\nPOST /test2\n \n{ \"message\":\"OK\" }\n");

            moqFileService
                .Setup(fs => fs.ReadAllTextAsync("testFile2.http", It.IsAny<CancellationToken>()))
                .ReturnsAsync("GET http://anyurl.com/test3/{{paramId}}?param=value\nheader1:val1\n \n{ \"message\":\"OK\" }\n###\nPUT /test4\n \n{ \"message\":\"OK\" }\n");
            moqFileService.Setup(fs => fs.GetFileName(It.IsAny<string>()))
                .Returns((string path) => Path.GetFileName(path));
            
            var uri = new Uri(url);
            
            // Act
            var result = service.GetHttpFilesInfo();
            // Assert
            Assert.True(result?.Any(), "File should exist");
            Assert.NotNull(result);
            var httpContext = new DefaultHttpContext()
            {
                Request =
                {
                    Method = method,
                    Path = uri.AbsolutePath,
                    QueryString = new QueryString(uri.Query),
                    Headers = { { "Host", uri.Host } }
                }
            };
            HttpRequest request = httpContext.Request;
            var validUrlResult = service.ValidateUrl(request, out var route, out var outParams);
            Assert.Equal(expectedResult, validUrlResult);
        }

    }
}