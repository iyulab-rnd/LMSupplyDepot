using LMSupplyDepots.Tools.HuggingFace.Client;
using LMSupplyDepots.Tools.HuggingFace.Models;
using LMSupplyDepots.Tools.HuggingFace.Tests.Core;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;

namespace LMSupplyDepots.Tools.HuggingFace.Tests;

public class HuggingFaceClientTests
{
    private readonly Mock<ILogger<HuggingFaceClient>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly HuggingFaceClientOptions _options;
    private readonly MockHttpMessageHandler _mockHandler;
    private readonly HuggingFaceClient _client;  // IHuggingFaceClient 대신 구체 타입

    public HuggingFaceClientTests()
    {
        _loggerMock = new Mock<ILogger<HuggingFaceClient>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _options = new HuggingFaceClientOptions
        {
            Token = "test_token",
            MaxRetries = 1
        };

        _mockHandler = new MockHttpMessageHandler();
        _client = new HuggingFaceClient(_options, _mockHandler, _loggerFactoryMock.Object);
    }

    [Fact]
    public async Task SearchModelsAsync_WithValidParameters_ReturnsGgufModels()
    {
        // Act
        var result = await _client.SearchModelsAsync(
            search: "test",
            filters: ["test-filter"],
            limit: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);  // GGUF 파일이 있는 모델만 반환
        foreach (var model in result)
        {
            Assert.True(model.HasGgufFiles());  // GGUF 파일 존재 확인
            Assert.NotNull(model.ID);
            Assert.NotNull(model.ModelId);
        }
    }

    [Fact]
    public async Task SearchModelsAsync_WithNoParameters_ReturnsGgufModels()
    {
        // Act
        var result = await _client.SearchModelsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, model => Assert.True(model.HasGgufFiles()));
    }

    public async Task FindModelByRepoIdAsync_WithValidId_ReturnsModel()
    {
        // Arrange
        var repoId = "test/model";

        // Act
        var result = await _client.FindModelByRepoIdAsync(repoId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(repoId, result.ID);
        Assert.NotNull(result.Siblings);
    }

    [Fact]
    public async Task FindModelByRepoIdAsync_WithInvalidId_ThrowsException()
    {
        // Arrange
        var repoId = "nonexistent/model";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HuggingFaceException>(
            () => _client.FindModelByRepoIdAsync(repoId));
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetFileInfoAsync_WithValidPath_ReturnsFileInfo()
    {
        // Arrange
        var repoId = "test/model";
        var filePath = "config.json";

        // Act
        var result = await _client.GetFileInfoAsync(repoId, filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("config.json", result.Name);
        Assert.Equal(filePath, result.Path);
        Assert.NotNull(result.Size);
        Assert.NotNull(result.MimeType);
    }

    [Fact]
    public async Task GetFileInfoAsync_WithInvalidPath_ThrowsException()
    {
        // Arrange
        var repoId = "test/model";
        var filePath = "nonexistent.txt";

        // Act & Assert
        await Assert.ThrowsAsync<HuggingFaceException>(
            () => _client.GetFileInfoAsync(repoId, filePath));
    }

    [Fact]
    public async Task DownloadFileAsync_WithValidFile_DownloadsSuccessfully()
    {
        // Arrange
        var repoId = "test/model";
        var filePath = "test.bin";
        var outputPath = Path.GetTempFileName();

        try
        {
            // Act
            await foreach (var progress in _client.DownloadFileAsync(repoId, filePath, outputPath))
            {
                // Assert progress
                Assert.NotNull(progress);
                Assert.Equal(outputPath, progress.UploadPath);
                Assert.True(progress.CurrentBytes >= 0);
                Assert.True(progress.DownloadSpeed >= 0);
                if (progress.TotalBytes.HasValue)
                {
                    Assert.True(progress.TotalBytes > 0);
                }
            }

            // Assert file exists
            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }

    [Fact]
    public void Dispose_CallsDispose_OnlyOnce()
    {
        // Act
        _client.Dispose();
        _client.Dispose(); // Second call should not throw

        // Assert - no exception thrown
    }
}
