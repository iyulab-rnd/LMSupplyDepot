using LMSupplyDepots.LLamaEngine.Models;
using LMSupplyDepots.LLamaEngine.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace LMSupplyDepots.LLamaEngine.Tests;


public class LocalModelManagerTests
{
    private readonly Mock<ILogger<LocalModelManager>> _loggerMock;
    private readonly LocalModelManager _manager;
    private readonly string _testModelPath;

    public LocalModelManagerTests()
    {
        _loggerMock = new Mock<ILogger<LocalModelManager>>();
        _manager = new LocalModelManager(_loggerMock.Object);
        _testModelPath = Path.Combine(Path.GetTempPath(), "test_model.gguf");

        // 테스트용 임시 파일 생성
        File.WriteAllText(_testModelPath, "dummy content");
    }

    [Fact]
    public async Task LoadModelAsync_UpdatesModelState()
    {
        // Arrange
        var modelIdentifier = "provider/model:model.gguf";

        // Act
        var result = await _manager.LoadModelAsync(_testModelPath, modelIdentifier);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(LocalModelState.Loaded, result.State);
        Assert.Equal("provider", result.Provider);
        Assert.Equal("model", result.ModelName);
        Assert.Equal("model.gguf", result.FileName);
    }

    [Fact]
    public async Task LoadModelAsync_HandlesFailure()
    {
        // Arrange
        var filePath = "/nonexistent/path/model.gguf";
        var modelIdentifier = "provider/model:model.gguf";

        // Act
        var result = await _manager.LoadModelAsync(filePath, modelIdentifier);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(LocalModelState.Failed, result.State);
        Assert.NotNull(result.LastError);
    }

    [Fact]
    public async Task UnloadModelAsync_UpdatesModelState()
    {
        // Arrange
        var filePath = "/path/to/model.gguf";
        var modelIdentifier = "provider/model:model.gguf";
        await _manager.LoadModelAsync(filePath, modelIdentifier);

        // Act
        await _manager.UnloadModelAsync(modelIdentifier);

        // Assert
        var modelInfo = await _manager.GetModelInfoAsync(modelIdentifier);
        Assert.NotNull(modelInfo);
        Assert.Equal(LocalModelState.Unloaded, modelInfo.State);
    }

    [Fact]
    public async Task GetLoadedModelsAsync_ReturnsOnlyLoadedModels()
    {
        // Arrange
        var modelIdentifier1 = "provider/model1:model1.gguf";
        var modelIdentifier2 = "provider/model2:model2.gguf";

        await _manager.LoadModelAsync(_testModelPath, modelIdentifier1);
        var failedModelPath = Path.Combine(Path.GetTempPath(), "nonexistent.gguf");
        await _manager.LoadModelAsync(failedModelPath, modelIdentifier2);

        // Act
        var loadedModels = await _manager.GetLoadedModelsAsync();

        // Assert
        Assert.Single(loadedModels);
        Assert.Equal(modelIdentifier1, loadedModels[0].GetFullIdentifier());
    }

    public void Dispose()
    {
        // 테스트 후 임시 파일 정리
        if (File.Exists(_testModelPath))
        {
            File.Delete(_testModelPath);
        }
    }
}
