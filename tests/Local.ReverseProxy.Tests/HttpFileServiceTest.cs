using Local.ReverseProxy.Services;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
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
                .ReturnsAsync("GET /test3\n \n{ \"message\":\"OK\" }\n###\nPUT /test4\n \n{ \"message\":\"OK\" }\n");
            moqFileService.Setup(fs => fs.GetFileName(It.IsAny<string>()))
                .Returns((string path) => Path.GetFileName(path));

            // Act
            var files = service.GetHttpFilesInfo();
            // Assert
            Assert.True(files != null, "File should exist");
        }

    }
}