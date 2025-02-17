using LLama;
using LMSupplyDepots.LLamaEngine.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LMSupplyDepots.LLamaEngine.Services;

public interface ILocalModelManager
{
    event EventHandler<ModelStateChangedEventArgs>? ModelStateChanged;
    Task<LocalModelInfo?> LoadModelAsync(string filePath, string modelIdentifier);
    Task UnloadModelAsync(string modelIdentifier);
    LLamaWeights? GetModelWeights(string modelIdentifier);
    Task<IReadOnlyList<LocalModelInfo>> GetLoadedModelsAsync();
    Task<LocalModelInfo?> GetModelInfoAsync(string modelIdentifier);
    string NormalizeModelIdentifier(string modelIdentifier);
}

public class LocalModelManager : ILocalModelManager
{
    private readonly ILogger<LocalModelManager> _logger;
    private readonly ConcurrentDictionary<string, LocalModelInfo> _localModels = new();
    private readonly ConcurrentDictionary<string, LLamaWeights> _weights = new();
    private readonly ILLamaBackendService _backendService;

    public event EventHandler<ModelStateChangedEventArgs>? ModelStateChanged;

    public LocalModelManager(
        ILogger<LocalModelManager> logger,
        ILLamaBackendService backendService)
    {
        _logger = logger;
        _backendService = backendService;
    }

    public string NormalizeModelIdentifier(string modelIdentifier)
    {
        if (modelIdentifier.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
        {
            return modelIdentifier[..^5];
        }
        return modelIdentifier;
    }

    public async Task<LocalModelInfo?> LoadModelAsync(string filePath, string modelIdentifier)
    {
        modelIdentifier = NormalizeModelIdentifier(modelIdentifier);
        var modelInfo = LocalModelInfo.CreateFromIdentifier(filePath, modelIdentifier);

        if (_localModels.TryGetValue(modelIdentifier, out var existingInfo) &&
            existingInfo.State == LocalModelState.Loaded)
        {
            return existingInfo;
        }

        UpdateModelState(modelInfo, LocalModelState.Loading);

        try
        {
            var parameters = _backendService.GetOptimalModelParams(filePath);
            var weights = await LLamaWeights.LoadFromFileAsync(parameters);
            _weights[modelIdentifier] = weights;

            UpdateModelState(modelInfo, LocalModelState.Loaded);
            return modelInfo;
        }
        catch (Exception ex)
        {
            modelInfo.LastError = ex.Message;
            UpdateModelState(modelInfo, LocalModelState.Failed);
            throw;
        }
    }

    public LLamaWeights? GetModelWeights(string modelIdentifier)
    {
        _weights.TryGetValue(modelIdentifier, out var weights);
        return weights;
    }
    
    private void UpdateModelState(LocalModelInfo modelInfo, LocalModelState newState)
    {
        var oldState = modelInfo.State;
        modelInfo.State = newState;
        _localModels[modelInfo.ModelId] = modelInfo;

        ModelStateChanged?.Invoke(this, new ModelStateChangedEventArgs(
            modelInfo.ModelId,
            oldState,
            newState));
    }

    public async Task UnloadModelAsync(string modelIdentifier)
    {
        modelIdentifier = NormalizeModelIdentifier(modelIdentifier);

        if (_localModels.TryGetValue(modelIdentifier, out var modelInfo))
        {
            try
            {
                UpdateModelState(modelInfo, LocalModelState.Unloading);
                UpdateModelState(modelInfo, LocalModelState.Unloaded);

                _logger.LogInformation("Successfully unloaded model {ModelIdentifier}", modelIdentifier);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to unload model {ModelIdentifier}", modelIdentifier);
                modelInfo.LastError = ex.Message;
                UpdateModelState(modelInfo, LocalModelState.Failed);
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
        modelIdentifier = NormalizeModelIdentifier(modelIdentifier);
        _localModels.TryGetValue(modelIdentifier, out var modelInfo);
        return Task.FromResult(modelInfo);
    }
}