using LLama;
using LLama.Common;
using LMSupplyDepots.LLamaEngine.Models;
using LMSupplyDepots.LLamaEngine.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace LMSupplyDepots.LLamaEngine.Tests;

public class LocalLLMServiceTests
{
    private readonly Mock<ILogger<LocalLLMService>> _loggerMock;
    private readonly Mock<ILocalModelManager> _modelManagerMock;
    private readonly Mock<ILLamaBackendService> _backendServiceMock;
    private readonly LocalLLMService _service;
    private readonly string _testModelIdentifier = "test/model:file.gguf";

    public LocalLLMServiceTests()
    {
        _loggerMock = new Mock<ILogger<LocalLLMService>>();
        _modelManagerMock = new Mock<ILocalModelManager>();
        _backendServiceMock = new Mock<ILLamaBackendService>();

        SetupDefaultMocks();

        _service = new LocalLLMService(
            _loggerMock.Object,
            _modelManagerMock.Object,
            _backendServiceMock.Object);
    }

    private void SetupDefaultMocks()
    {
        var modelInfo = new LocalModelInfo
        {
            Provider = "test",
            ModelName = "model",
            FileName = "file.gguf",
            FullPath = Path.Combine(AppContext.BaseDirectory, "test.gguf"),
            State = LocalModelState.Loaded
        };

        _modelManagerMock.Setup(x => x.GetModelInfoAsync(_testModelIdentifier))
            .ReturnsAsync(modelInfo);

        _backendServiceMock.Setup(x => x.GetOptimalModelParams(It.IsAny<string>()))
            .Returns(new ModelParams("test.gguf"));
    }

    [Fact]
    public async Task InferAsync_ThrowsException_WhenModelNotFound()
    {
        // Arrange
        _modelManagerMock.Setup(x => x.GetModelInfoAsync(_testModelIdentifier))
            .ReturnsAsync((LocalModelInfo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InferAsync(_testModelIdentifier, "test prompt"));
    }

    [Fact]
    public async Task InferAsync_ThrowsException_WhenModelNotLoaded()
    {
        // Arrange
        var modelInfo = new LocalModelInfo
        {
            Provider = "test",
            ModelName = "model",
            FileName = "file.gguf",
            FullPath = "/path/to/model",
            State = LocalModelState.Unloaded
        };

        _modelManagerMock.Setup(x => x.GetModelInfoAsync(_testModelIdentifier))
            .ReturnsAsync(modelInfo);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.InferAsync(_testModelIdentifier, "test prompt"));
    }

    [Fact]
    public async Task InferStreamAsync_ThrowsException_WhenModelNotFound()
    {
        // Arrange
        _modelManagerMock.Setup(x => x.GetModelInfoAsync(_testModelIdentifier))
            .ReturnsAsync((LocalModelInfo?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in _service.InferStreamAsync(_testModelIdentifier, "test prompt"))
            {
                // This should not execute
            }
        });
    }

    [Fact]
    public async Task CreateEmbeddingAsync_ThrowsException_WhenModelNotLoaded()
    {
        // Arrange
        var modelInfo = new LocalModelInfo
        {
            Provider = "test",
            ModelName = "model",
            FileName = "file.gguf",
            FullPath = "/path/to/model",
            State = LocalModelState.Unloaded
        };

        _modelManagerMock.Setup(x => x.GetModelInfoAsync(_testModelIdentifier))
            .ReturnsAsync(modelInfo);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateEmbeddingAsync(_testModelIdentifier, "test text"));
    }
}
