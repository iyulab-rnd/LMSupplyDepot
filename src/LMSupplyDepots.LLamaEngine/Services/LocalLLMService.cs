using LLama;
using LLama.Common;
using LMSupplyDepots.LLamaEngine.Models;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace LMSupplyDepots.LLamaEngine.Services;

public interface ILocalLLMService
{
    Task<string> InferAsync(
        string modelIdentifier,
        string prompt,
        InferenceParams? parameters = null);

    IAsyncEnumerable<string> InferStreamAsync(
        string modelIdentifier,
        string prompt,
        InferenceParams? parameters = null);

    Task<float[]> CreateEmbeddingAsync(
        string modelIdentifier,
        string text,
        bool normalize = true);
}

public class LocalLLMService : ILocalLLMService, IAsyncDisposable
{
    private readonly ILogger<LocalLLMService> _logger;
    private readonly ILocalModelManager _modelManager;
    private readonly ILLamaBackendService _backendService;
    private readonly ConcurrentDictionary<string, ModelResources> _loadedModels = new();

    private class ModelResources : IDisposable
    {
        public LLamaWeights Weights { get; }
        public LLamaContext Context { get; }
        public ChatSession Session { get; }

        public ModelResources(LLamaWeights weights, LLamaContext context)
        {
            Weights = weights;
            Context = context;
            var executor = new InteractiveExecutor(context);
            Session = new ChatSession(executor);
        }

        public void Dispose()
        {
            Context.Dispose();
            Weights.Dispose();
        }
    }

    public LocalLLMService(
        ILogger<LocalLLMService> logger,
        ILocalModelManager modelManager,
        ILLamaBackendService backendService)
    {
        _logger = logger;
        _modelManager = modelManager;
        _backendService = backendService;
    }

    public async Task<string> InferAsync(
        string modelIdentifier,
        string prompt,
        InferenceParams? parameters = null)
    {
        var resources = await GetModelResourcesAsync(modelIdentifier);
        parameters ??= new InferenceParams { MaxTokens = 2048 };

        var result = new System.Text.StringBuilder();
        await foreach (var text in resources.Session.ChatAsync(
            new ChatHistory.Message(AuthorRole.User, prompt), parameters))
        {
            result.Append(text);
        }

        return result.ToString();
    }

    public async IAsyncEnumerable<string> InferStreamAsync(
        string modelIdentifier,
        string prompt,
        InferenceParams? parameters = null)
    {
        var resources = await GetModelResourcesAsync(modelIdentifier);
        parameters ??= new InferenceParams { MaxTokens = 2048 };

        await foreach (var text in resources.Session.ChatAsync(
            new ChatHistory.Message(AuthorRole.User, prompt), parameters))
        {
            yield return text;
        }
    }

    public async Task<float[]> CreateEmbeddingAsync(
        string modelIdentifier,
        string text,
        bool normalize = true)
    {
        var modelInfo = await _modelManager.GetModelInfoAsync(modelIdentifier);
        if (modelInfo?.State != LocalModelState.Loaded)
        {
            throw new InvalidOperationException($"Model {modelIdentifier} is not loaded");
        }

        var parameters = _backendService.GetOptimalModelParams(modelInfo.FullPath);
        parameters.Embeddings = true;

        var resources = await GetModelResourcesAsync(modelIdentifier);
        using var embedder = new LLamaEmbedder(resources.Weights, parameters);
        var embeddings = await embedder.GetEmbeddings(text);

        if (normalize && embeddings.Count > 0)
        {
            return NormalizeVector(embeddings[0]);
        }

        return embeddings[0];
    }

    private async Task<ModelResources> GetModelResourcesAsync(string modelIdentifier)
    {
        if (_loadedModels.TryGetValue(modelIdentifier, out var resources))
        {
            return resources;
        }

        var modelInfo = await _modelManager.GetModelInfoAsync(modelIdentifier);
        if (modelInfo?.State != LocalModelState.Loaded)
        {
            throw new InvalidOperationException($"Model {modelIdentifier} is not loaded");
        }

        var parameters = _backendService.GetOptimalModelParams(modelInfo.FullPath);
        var weights = await LLamaWeights.LoadFromFileAsync(parameters);
        var context = new LLamaContext(weights, parameters);

        resources = new ModelResources(weights, context);

        if (!_loadedModels.TryAdd(modelIdentifier, resources))
        {
            resources.Dispose();
            throw new InvalidOperationException($"Failed to initialize model {modelIdentifier}");
        }

        return resources;
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

    public async ValueTask DisposeAsync()
    {
        foreach (var (modelId, resources) in _loadedModels)
        {
            try
            {
                resources.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing model {ModelIdentifier}", modelId);
            }
        }

        _loadedModels.Clear();
    }
}