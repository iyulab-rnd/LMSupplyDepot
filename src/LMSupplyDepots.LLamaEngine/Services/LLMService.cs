using LLama;
using LLama.Common;
using LMSupplyDepots.LLamaEngine.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace LMSupplyDepots.LLamaEngine.Services;

public interface ILLMService
{
    Task<string> InferAsync(
        string modelIdentifier,
        string prompt,
        InferenceParams? parameters = null,
        CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> InferStreamAsync(
        string modelIdentifier,
        string prompt,
        InferenceParams? parameters = null,
        CancellationToken cancellationToken = default);

    Task<float[]> CreateEmbeddingAsync(
        string modelIdentifier,
        string text,
        bool normalize = true,
        CancellationToken cancellationToken = default);
}

public class LLMService : ILLMService, IAsyncDisposable
{
    private readonly ILogger<LLMService> _logger;
    private readonly ILLamaModelManager _modelManager;
    private readonly ILLamaBackendService _backendService;
    private readonly ConcurrentDictionary<string, ModelResources> _loadedModels = new();
    private readonly ConcurrentDictionary<string, LLamaContext> _contexts = new();
    private readonly ConcurrentDictionary<string, InteractiveExecutor> _executors = new();
    private bool _disposed;

    public LLMService(
        ILogger<LLMService> logger,
        ILLamaModelManager modelManager,
        ILLamaBackendService backendService)
    {
        _logger = logger;
        _modelManager = modelManager;
        _backendService = backendService;
        _modelManager.ModelStateChanged += OnModelStateChanged;
    }

    private void OnModelStateChanged(object? sender, ModelStateChangedEventArgs e)
    {
        if (e.NewState == LocalModelState.Unloading || e.NewState == LocalModelState.Unloaded)
        {
            CleanupModelResources(e.ModelIdentifier);
        }
    }

    private void CleanupModelResources(string modelId)
    {
        if (_executors.TryRemove(modelId, out var executor))
        {
            try
            {
                // Executor will be disposed when context is disposed
                _logger.LogInformation("Removed executor for model {ModelId}", modelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up executor for model {ModelId}", modelId);
            }
        }

        if (_contexts.TryRemove(modelId, out var context))
        {
            try
            {
                context.Dispose();
                _logger.LogInformation("Disposed context for model {ModelId}", modelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing context for model {ModelId}", modelId);
            }
        }

        if (_loadedModels.TryRemove(modelId, out var resources))
        {
            try
            {
                resources.Dispose();
                _logger.LogInformation("Disposed resources for model {ModelId}", modelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing resources for model {ModelId}", modelId);
            }
        }
    }

    private async Task<(InteractiveExecutor Executor, ModelParams Parameters)> GetExecutorAsync(
        string modelIdentifier,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        modelIdentifier = _modelManager.NormalizeModelIdentifier(modelIdentifier);

        if (_executors.TryGetValue(modelIdentifier, out var executor))
        {
            var modelInfo = await _modelManager.GetModelInfoAsync(modelIdentifier);
            if (modelInfo?.State == LocalModelState.Loaded)
            {
                var parameters = _backendService.GetOptimalModelParams(modelInfo.FullPath);
                return (executor, parameters);
            }
        }

        var info = await _modelManager.GetModelInfoAsync(modelIdentifier);
        if (info?.State != LocalModelState.Loaded)
        {
            throw new InvalidOperationException($"Model {modelIdentifier} is not loaded");
        }

        var weights = _modelManager.GetModelWeights(modelIdentifier);
        if (weights == null)
        {
            throw new InvalidOperationException($"Model weights not found for {modelIdentifier}");
        }

        var modelParams = _backendService.GetOptimalModelParams(info.FullPath);
        var context = new LLamaContext(weights, modelParams);

        if (!_contexts.TryAdd(modelIdentifier, context))
        {
            context.Dispose();
            throw new InvalidOperationException($"Failed to create context for model {modelIdentifier}");
        }

        executor = new InteractiveExecutor(context);
        if (!_executors.TryAdd(modelIdentifier, executor))
        {
            throw new InvalidOperationException($"Failed to create executor for model {modelIdentifier}");
        }

        return (executor, modelParams);
    }

    public async Task<string> InferAsync(
        string modelIdentifier,
        string prompt,
        InferenceParams? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var (executor, _) = await GetExecutorAsync(modelIdentifier, cancellationToken);
        parameters ??= ParameterFactory.NewInferenceParams();

        try
        {
            var result = new System.Text.StringBuilder();
            await foreach (var text in executor.InferAsync(prompt, parameters).WithCancellation(cancellationToken))
            {
                result.Append(text);
            }
            return result.ToString();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during inference for model {ModelIdentifier}", modelIdentifier);
            throw;
        }
    }

    public async IAsyncEnumerable<string> InferStreamAsync(
        string modelIdentifier,
        string prompt,
        InferenceParams? parameters = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var (executor, _) = await GetExecutorAsync(modelIdentifier, cancellationToken);
        parameters ??= ParameterFactory.NewInferenceParams();

        await foreach (var text in executor.InferAsync(prompt, parameters, cancellationToken)
            .WithCancellation(cancellationToken))
        {
            yield return text;
        }
    }

    public async Task<float[]> CreateEmbeddingAsync(
        string modelIdentifier,
        string text,
        bool normalize = true,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        var (_, modelParams) = await GetExecutorAsync(modelIdentifier, cancellationToken);
        modelParams.Embeddings = true;

        var resources = _loadedModels[modelIdentifier];
        try
        {
            using var embedder = new LLamaEmbedder(resources.Weights, modelParams);
            var embeddings = await embedder.GetEmbeddings(text);

            if (normalize && embeddings.Count > 0)
            {
                return NormalizeVector(embeddings[0]);
            }

            return embeddings[0];
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating embedding for model {ModelIdentifier}", modelIdentifier);
            throw;
        }
    }

    private float[] NormalizeVector(float[] vector)
    {
        float magnitude = (float)Math.Sqrt(vector.Sum(x => x * x));
        if (magnitude > 0)
        {
            for (int i = 0; i < vector.Length; i++)
            {
                vector[i] /= magnitude;
            }
        }
        return vector;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(LLMService));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _disposed = true;
        _modelManager.ModelStateChanged -= OnModelStateChanged;

        var exceptions = new List<Exception>();

        foreach (var modelId in _loadedModels.Keys)
        {
            try
            {
                CleanupModelResources(modelId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup of model {ModelId}", modelId);
                exceptions.Add(ex);
            }
        }

        _loadedModels.Clear();
        _contexts.Clear();
        _executors.Clear();

        if (exceptions.Count == 1)
        {
            throw exceptions[0];
        }
        else if (exceptions.Count > 1)
        {
            throw new AggregateException("Multiple errors occurred during disposal", exceptions);
        }

        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }
}