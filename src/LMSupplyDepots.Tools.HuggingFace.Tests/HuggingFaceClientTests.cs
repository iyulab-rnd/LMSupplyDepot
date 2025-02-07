using LMSupplyDepots.Tools.HuggingFace.Client;
using LMSupplyDepots.Tools.HuggingFace.Common;
using LMSupplyDepots.Tools.HuggingFace.Download;
using LMSupplyDepots.Tools.HuggingFace.Models;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LMSupplyDepots.Tools.HuggingFace.Tests;

public class HuggingFaceClientTests
{
    private readonly Mock<ILogger<HuggingFaceClient>> _loggerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly HuggingFaceClientOptions _options;
    private readonly HttpMessageHandler _mockHandler;
    private readonly HuggingFaceClient _client;

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
        var httpClient = new HttpClient(_mockHandler);
        _client = new HuggingFaceClient(_options, _loggerFactoryMock.Object);
    }

    [Fact]
    public async Task SearchModelsAsync_Success()
    {
        // Arrange
        var expectedModels = new[]
        {
            new HuggingFaceModel { ID = "model1" },
            new HuggingFaceModel { ID = "model2" }
        };

        // Act
        var result = await _client.SearchModelsAsync(
            search: "test",
            filters: new[] { "filter1" },
            limit: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task FindModelByRepoIdAsync_Success()
    {
        // Arrange
        var repoId = "test/model";
        var expectedModel = new HuggingFaceModel { ID = repoId };

        // Act
        var result = await _client.FindModelByRepoIdAsync(repoId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(repoId, result.ID);
    }

    [Fact]
    public async Task FindModelByRepoIdAsync_NotFound()
    {
        // Arrange
        var repoId = "nonexistent/model";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HuggingFaceException>(
            () => _client.FindModelByRepoIdAsync(repoId));
        Assert.Equal(HttpStatusCode.NotFound, exception.StatusCode);
    }

    [Fact]
    public async Task GetFileInfoAsync_Success()
    {
        // Arrange
        var repoId = "test/model";
        var filePath = "config.json";

        // Act
        var result = await _client.GetFileInfoAsync(repoId, filePath);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("config.json", result.Name);
    }

    [Fact]
    public async Task DownloadFileAsync_Success()
    {
        // Arrange
        var repoId = "test/model";
        var filePath = "model.bin";
        var outputPath = Path.GetTempFileName();

        try
        {
            // Act
            await foreach (var progress in _client.DownloadFileAsync(repoId, filePath, outputPath))
            {
                // Assert progress
                Assert.NotNull(progress);
                Assert.True(progress.CurrentBytes >= 0);
                Assert.True(progress.DownloadSpeed >= 0);
            }

            // Assert file exists
            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            // Cleanup
            if (File.Exists(outputPath))
                File.Delete(outputPath);
        }
    }
}

public class HuggingFaceClientOptionsTests
{
    [Fact]
    public void Constructor_DefaultValues()
    {
        // Act
        var options = new HuggingFaceClientOptions();

        // Assert
        Assert.Null(options.Token);
        Assert.Equal(5, options.MaxConcurrentDownloads);
        Assert.Equal(100, options.ProgressUpdateInterval);
        Assert.Equal(TimeSpan.FromMinutes(5), options.Timeout);
        Assert.Equal(8192, options.BufferSize);
        Assert.Equal(3, options.MaxRetries);
        Assert.Equal(1000, options.RetryDelayMilliseconds);
    }

    [Fact]
    public void Validate_ValidOptions_NoException()
    {
        // Arrange
        var options = new HuggingFaceClientOptions
        {
            Token = "test_token"
        };

        // Act & Assert
        options.Validate();
    }

    [Theory]
    [InlineData(0)]  // Too low
    [InlineData(21)] // Too high
    public void MaxConcurrentDownloads_InvalidValue_ThrowsArgumentException(int value)
    {
        // Arrange
        var options = new HuggingFaceClientOptions();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => options.MaxConcurrentDownloads = value);
    }

    [Fact]
    public void Reset_ResetsAllValues()
    {
        // Arrange
        var options = new HuggingFaceClientOptions
        {
            Token = "test_token",
            MaxConcurrentDownloads = 10,
            ProgressUpdateInterval = 200,
            MaxRetries = 2
        };

        // Act
        options.Reset();

        // Assert
        Assert.Null(options.Token);
        Assert.Equal(5, options.MaxConcurrentDownloads);
        Assert.Equal(100, options.ProgressUpdateInterval);
        Assert.Equal(3, options.MaxRetries);
    }
}

public class StringFormatterTests
{
    [Theory]
    [InlineData(0, "0 B")]
    [InlineData(1023, "1023 B")]
    [InlineData(1024, "1 KB")]
    [InlineData(1048576, "1 MB")]
    [InlineData(1073741824, "1 GB")]
    public void FormatSize_ReturnsCorrectFormat(long bytes, string expected)
    {
        // Act
        var result = StringFormatter.FormatSize(bytes);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(-1)]
    public void FormatSize_NegativeValue_ThrowsArgumentException(long bytes)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => StringFormatter.FormatSize(bytes));
    }

    [Fact]
    public void FormatTimeSpan_ValidTimeSpan_ReturnsFormattedString()
    {
        // Arrange
        var timeSpan = TimeSpan.FromHours(1.5);

        // Act
        var result = StringFormatter.FormatTimeSpan(timeSpan);

        // Assert
        Assert.Equal("01:30:00", result);
    }

    [Fact]
    public void FormatTimeSpan_NullTimeSpan_ReturnsUnknown()
    {
        // Act
        var result = StringFormatter.FormatTimeSpan(null);

        // Assert
        Assert.Equal("Unknown", result);
    }

    [Theory]
    [InlineData(0, "0%")]
    [InlineData(0.5, "50%")]
    [InlineData(1, "100%")]
    public void FormatProgress_ValidProgress_ReturnsFormattedString(double progress, string expected)
    {
        // Act
        var result = StringFormatter.FormatProgress(progress);

        // Assert
        Assert.Equal(expected, result);
    }
}

public class HuggingFaceModelTests
{
    [Fact]
    public void GetFilePaths_NoPattern_ReturnsAllFiles()
    {
        // Arrange
        var model = new HuggingFaceModel
        {
            Siblings = new[]
            {
                new ModelResource { Rfilename = "config.json" },
                new ModelResource { Rfilename = "model.bin" },
                new ModelResource { Rfilename = "vocab.txt" }
            }
        };

        // Act
        var result = model.GetFilePaths();

        // Assert
        Assert.Equal(3, result.Length);
        Assert.Contains("config.json", result);
        Assert.Contains("model.bin", result);
        Assert.Contains("vocab.txt", result);
    }

    [Fact]
    public void GetFilePaths_WithPattern_ReturnsMatchingFiles()
    {
        // Arrange
        var model = new HuggingFaceModel
        {
            Siblings = new[]
            {
                new ModelResource { Rfilename = "config.json" },
                new ModelResource { Rfilename = "model.bin" },
                new ModelResource { Rfilename = "vocab.txt" }
            }
        };
        var pattern = new Regex(@"\.json$");

        // Act
        var result = model.GetFilePaths(pattern);

        // Assert
        Assert.Single(result);
        Assert.Equal("config.json", result[0]);
    }
}

// Mock HTTP handler for testing
public class MockHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);

        if (request.RequestUri.PathAndQuery.Contains("/api/models"))
        {
            var models = new[]
            {
                new HuggingFaceModel { ID = "model1" },
                new HuggingFaceModel { ID = "model2" }
            };
            response.Content = new StringContent(JsonSerializer.Serialize(models));
        }
        else if (request.RequestUri.PathAndQuery.Contains("/nonexistent"))
        {
            response.StatusCode = HttpStatusCode.NotFound;
        }
        else if (request.Method == HttpMethod.Head)
        {
            response.Content = new StringContent("");
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            response.Content.Headers.ContentLength = 1000;
        }
        else
        {
            response.Content = new StringContent("test content");
        }

        return Task.FromResult(response);
    }
}

public class FileDownloadProgressTests
{
    [Fact]
    public void CreateCompleted_ReturnsCompletedProgress()
    {
        // Arrange
        var path = "test.file";
        var size = 1000L;

        // Act
        var progress = FileDownloadProgress.CreateCompleted(path, size);

        // Assert
        Assert.True(progress.IsCompleted);
        Assert.Equal(path, progress.UploadPath);
        Assert.Equal(size, progress.CurrentBytes);
        Assert.Equal(size, progress.TotalBytes);
        Assert.Equal(1.0, progress.DownloadProgress);
    }

    [Fact]
    public void CreateProgress_ReturnsProgressInfo()
    {
        // Arrange
        var path = "test.file";
        var current = 500L;
        var total = 1000L;
        var speed = 100.0;

        // Act
        var progress = FileDownloadProgress.CreateProgress(path, current, total, speed);

        // Assert
        Assert.False(progress.IsCompleted);
        Assert.Equal(path, progress.UploadPath);
        Assert.Equal(current, progress.CurrentBytes);
        Assert.Equal(total, progress.TotalBytes);
        Assert.Equal(0.5, progress.DownloadProgress);
    }
}

public class RepoDownloadProgressTests
{
    [Fact]
    public void Create_InitializesCorrectly()
    {
        // Arrange
        var files = new[] { "file1.txt", "file2.txt" };

        // Act
        var progress = RepoDownloadProgress.Create(files);

        // Assert
        Assert.False(progress.IsCompleted);
        Assert.Equal(2, progress.TotalFiles.Count);
        Assert.Empty(progress.CompletedFiles);
        Assert.Empty(progress.CurrentProgresses);
    }

    [Fact]
    public void WithProgress_UpdatesProgress()
    {
        // Arrange
        var files = new[] { "file1.txt", "file2.txt" };
        var progress = RepoDownloadProgress.Create(files);
        var completed = new[] { "file1.txt" };
        var current = new[]
        {
            FileDownloadProgress.CreateProgress("file2.txt", 50, 100, 10.0)
        };

        // Act
        var updated = progress.WithProgress(completed, current);

        // Assert
        Assert.Single(updated.CompletedFiles);
        Assert.Single(updated.CurrentProgresses);
        Assert.Equal(0.75, updated.TotalProgress); // One complete (0.5) + one half complete (0.25)
    }

    [Fact]
    public void AsCompleted_MarksAsComplete()
    {
        // Arrange
        var files = new[] { "file1.txt", "file2.txt" };
        var progress = RepoDownloadProgress.Create(files);

        // Act
        var completed = progress.AsCompleted();

        // Assert
        Assert.True(completed.IsCompleted);
        Assert.Equal(2, completed.CompletedFiles.Count);
        Assert.Empty(completed.CurrentProgresses);
        Assert.Equal(1.0, completed.TotalProgress);
    }
}