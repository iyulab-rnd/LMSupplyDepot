using LMSupplyDepots.LLamaEngine.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LMSupplyDepots.LLamaEngine.Services;

/// <summary>
/// 로컬 모델 파일의 생명주기와 상태를 관리합니다.
/// </summary>
public interface ILocalModelManager
{
    /// <summary>
    /// 특정 모델을 로드합니다.
    /// </summary>
    /// <param name="filePath">모델 파일의 전체 경로</param>
    /// <param name="modelIdentifier">모델 식별자 (예: provider/model:filename.gguf)</param>
    /// <returns>로드된 모델의 정보</returns>
    Task<LocalModelInfo?> LoadModelAsync(string filePath, string modelIdentifier);

    /// <summary>
    /// 로드된 모델을 언로드합니다.
    /// </summary>
    /// <param name="modelIdentifier">모델 식별자</param>
    Task UnloadModelAsync(string modelIdentifier);

    /// <summary>
    /// 현재 로드된 모든 모델의 목록을 반환합니다.
    /// </summary>
    Task<IReadOnlyList<LocalModelInfo>> GetLoadedModelsAsync();

    /// <summary>
    /// 특정 모델의 현재 상태 정보를 반환합니다.
    /// </summary>
    /// <param name="modelIdentifier">모델 식별자</param>
    Task<LocalModelInfo?> GetModelInfoAsync(string modelIdentifier);
}

public class LocalModelManager : ILocalModelManager
{
    private readonly ILogger<LocalModelManager> _logger;
    private readonly ConcurrentDictionary<string, LocalModelInfo> _localModels = new();

    public LocalModelManager(ILogger<LocalModelManager> logger)
    {
        _logger = logger;
    }

    public async Task<LocalModelInfo?> LoadModelAsync(string filePath, string modelIdentifier)
    {
        try
        {
            var modelInfo = LocalModelInfo.CreateFromIdentifier(filePath, modelIdentifier);

            if (_localModels.TryGetValue(modelIdentifier, out var existingInfo))
            {
                if (existingInfo.State == LocalModelState.Loaded)
                {
                    return existingInfo;
                }
            }

            modelInfo.State = LocalModelState.Loading;
            _localModels[modelIdentifier] = modelInfo;

            // 파일 존재 여부 확인
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Model file not found: {filePath}");
            }

            modelInfo.State = LocalModelState.Loaded;
            _localModels[modelIdentifier] = modelInfo;

            _logger.LogInformation("Successfully loaded model {ModelIdentifier} from {FilePath}",
                modelIdentifier, filePath);

            return modelInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load model {ModelIdentifier} from {FilePath}",
                modelIdentifier, filePath);

            var failedInfo = new LocalModelInfo
            {
                Provider = "unknown",
                ModelName = "unknown",
                FileName = Path.GetFileName(filePath),
                FullPath = filePath,
                State = LocalModelState.Failed,
                LastError = ex.Message
            };

            _localModels[modelIdentifier] = failedInfo;
            return failedInfo;
        }
    }

    public async Task UnloadModelAsync(string modelIdentifier)
    {
        if (_localModels.TryGetValue(modelIdentifier, out var modelInfo))
        {
            try
            {
                modelInfo.State = LocalModelState.Unloading;
                _localModels[modelIdentifier] = modelInfo;

                modelInfo.State = LocalModelState.Unloaded;
                _localModels[modelIdentifier] = modelInfo;

                _logger.LogInformation("Successfully unloaded model {ModelIdentifier}", modelIdentifier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unload model {ModelIdentifier}", modelIdentifier);
                modelInfo.State = LocalModelState.Failed;
                modelInfo.LastError = ex.Message;
                _localModels[modelIdentifier] = modelInfo;
                throw;
            }
        }
    }

    public Task<IReadOnlyList<LocalModelInfo>> GetLoadedModelsAsync()
    {
        var loadedModels = _localModels.Values
            .Where(m => m.State == LocalModelState.Loaded)
            .ToList();

        return Task.FromResult<IReadOnlyList<LocalModelInfo>>(loadedModels);
    }

    public Task<LocalModelInfo?> GetModelInfoAsync(string modelIdentifier)
    {
        _localModels.TryGetValue(modelIdentifier, out var modelInfo);
        return Task.FromResult(modelInfo);
    }
}