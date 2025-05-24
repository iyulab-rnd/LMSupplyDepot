using LMSupplyDepots.Models;
using LMSupplyDepots.ModelHub.Models;
using LMSupplyDepots.SDK;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using LMSupplyDepots.Contracts;

namespace LMSupplyDepots.Host;

/// <summary>
/// Hosting service for LMSupplyDepots operations that delegates functionality to LMSupplyDepot SDK
/// </summary>
public class HostService : IHostService, IAsyncDisposable
{
    private readonly ILogger<HostService> _logger;
    private readonly HostOptions _options;
    private readonly LMSupplyDepot _depot;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the HostService
    /// </summary>
    public HostService(ILogger<HostService> logger,
        IOptions<HostOptions> options)
    {
        _logger = logger;
        _options = options.Value;

        // Configure SDK options
        var sdkOptions = new LMSupplyDepotOptions
        {
            DataPath = _options.DataPath,
            MaxConcurrentDownloads = _options.MaxConcurrentDownloads,
            VerifyChecksums = _options.VerifyChecksums,
            MinimumFreeDiskSpace = _options.MinimumFreeDiskSpace,
            HuggingFaceApiToken = _options.HuggingFaceApiToken,
            DefaultTimeoutMs = _options.DefaultTimeoutMs,
            MaxConcurrentOperations = _options.MaxConcurrentOperations,
            EnableMetrics = _options.EnableMetrics,
            EnableModelCaching = _options.EnableModelCaching,
            MaxCachedModels = _options.MaxCachedModels,
            TempDirectory = _options.TempDirectory,
            ForceCpuOnly = _options.ForceCpuOnly
        };

        // Configure LLama options
        if (_options.LLamaOptions != null)
        {
            sdkOptions.LLamaOptions = new LLamaOptions
            {
                Threads = _options.LLamaOptions.Threads,
                GpuLayers = _options.LLamaOptions.GpuLayers,
                ContextSize = _options.LLamaOptions.ContextSize,
                BatchSize = _options.LLamaOptions.BatchSize,
                UseMemoryMapping = _options.LLamaOptions.UseMemoryMapping,
                Seed = _options.LLamaOptions.Seed
            };

            if (_options.LLamaOptions.AntiPrompt != null)
            {
                sdkOptions.LLamaOptions.AntiPrompt = new List<string>(_options.LLamaOptions.AntiPrompt);
            }
        }

        // Initialize the depot
        _depot = new LMSupplyDepot(sdkOptions, CreateLoggerFactory(logger));
        _logger.LogInformation("LMSupplyDepots Host Service initialized with models directory: {ModelsDirectory}", _options.DataPath);
    }

    private static ILoggerFactory CreateLoggerFactory(ILogger logger)
    {
        // Create a simple logger factory that forwards to the provided logger
        return LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new ForwardingLoggerProvider(logger));
        });
    }

    #region Model Management

    /// <summary>
    /// Lists available models with optional filtering
    /// </summary>
    public Task<IReadOnlyList<LMModel>> ListModelsAsync(ModelType? type = null, string? searchTerm = null, CancellationToken cancellationToken = default)
    {
        return _depot.ListModelsAsync(type, searchTerm, cancellationToken);
    }

    /// <summary>
    /// Gets a model by its ID
    /// </summary>
    public Task<LMModel?> GetModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return _depot.GetModelAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Checks if a model is loaded
    /// </summary>
    public Task<bool> IsModelLoadedAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return _depot.IsModelLoadedAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Gets a list of loaded models
    /// </summary>
    public Task<IReadOnlyList<LMModel>> GetLoadedModelsAsync(CancellationToken cancellationToken = default)
    {
        return _depot.GetLoadedModelsAsync(cancellationToken);
    }

    /// <summary>
    /// Loads a model
    /// </summary>
    public Task<LMModel> LoadModelAsync(string modelId, Dictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default)
    {
        return _depot.LoadModelAsync(modelId, parameters, cancellationToken);
    }

    /// <summary>
    /// Unloads a model
    /// </summary>
    public Task UnloadModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return _depot.UnloadModelAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Deletes a model
    /// </summary>
    public Task<bool> DeleteModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return _depot.DeleteModelAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Sets an alias for a model
    /// </summary>
    public Task<LMModel> SetModelAliasAsync(
        string modelId,
        string alias,
        CancellationToken cancellationToken = default)
    {
        return _depot.SetModelAliasAsync(modelId, alias, cancellationToken);
    }

    #endregion

    #region Model Download and Repository

    /// <summary>
    /// Searches for models from external sources
    /// </summary>
    public Task<IReadOnlyList<ModelSearchResult>> SearchModelsAsync(ModelType? type = null, string? searchTerm = null, int limit = 10, CancellationToken cancellationToken = default)
    {
        return _depot.SearchModelsAsync(type, searchTerm, limit, cancellationToken);
    }

    /// <summary>
    /// Downloads a model from an external source
    /// </summary>
    public Task<LMModel> DownloadModelAsync(string modelId, IProgress<ModelDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        return _depot.DownloadModelAsync(modelId, progress, cancellationToken);
    }

    /// <summary>
    /// Pauses an active model download
    /// </summary>
    public Task<bool> PauseDownloadAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return _depot.PauseDownloadAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Resumes a paused model download
    /// </summary>
    public Task<ModelDownloadState> ResumeDownloadAsync(string modelId, IProgress<ModelDownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        return _depot.ResumeDownloadAsync(modelId, progress, cancellationToken);
    }

    /// <summary>
    /// Cancels an active or paused model download
    /// </summary>
    public Task<bool> CancelDownloadAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return _depot.CancelDownloadAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Gets the current status of a model download
    /// </summary>
    public Task<ModelDownloadStatus?> GetDownloadStatusAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return _depot.GetDownloadStatusAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Gets information about all active downloads
    /// </summary>
    public IReadOnlyDictionary<string, ModelDownloadState> GetActiveDownloads()
    {
        return _depot.GetActiveDownloads();
    }

    /// <summary>
    /// Gets information about a model from an external source without downloading it
    /// </summary>
    public Task<LMModel> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return _depot.GetModelInfoAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Gets information about a model repository from an external source
    /// </summary>
    public Task<LMRepo> GetRepositoryInfoAsync(string repoId, CancellationToken cancellationToken = default)
    {
        return _depot.GetRepositoryInfoAsync(repoId, cancellationToken);
    }

    #endregion

    #region Text Generation

    /// <summary>
    /// Generates text using a loaded model
    /// </summary>
    public Task<GenerationResponse> GenerateTextAsync(string modelId, GenerationRequest request, CancellationToken cancellationToken = default)
    {
        return _depot.GenerateTextAsync(modelId, request, cancellationToken);
    }

    /// <summary>
    /// Generates text using a loaded model with streaming output
    /// </summary>
    public async IAsyncEnumerable<string> GenerateTextStreamAsync(
        string modelId,
        GenerationRequest request,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Force the request to be a streaming request
        request.Stream = true;

        await foreach (var token in _depot.GenerateTextStreamAsync(
            modelId,
            request.Prompt,
            request.MaxTokens,
            request.Temperature,
            request.TopP,
            request.Parameters,
            cancellationToken))
        {
            yield return token;
        }
    }

    /// <summary>
    /// Generates text using a loaded model with simple parameters
    /// </summary>
    public Task<GenerationResponse> GenerateTextAsync(
        string modelId,
        string prompt,
        int maxTokens = 256,
        float temperature = 0.7f,
        float topP = 0.95f,
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return _depot.GenerateTextAsync(
            modelId,
            prompt,
            maxTokens,
            temperature,
            topP,
            parameters,
            cancellationToken);
    }

    #endregion

    #region Embeddings

    /// <summary>
    /// Generates embeddings for texts using a loaded model with a full request
    /// </summary>
    public Task<EmbeddingResponse> GenerateEmbeddingsAsync(
        string modelId,
        EmbeddingRequest request,
        CancellationToken cancellationToken = default)
    {
        return _depot.GenerateEmbeddingsAsync(modelId, request, cancellationToken);
    }

    /// <summary>
    /// Generates embeddings for texts using a loaded model
    /// </summary>
    public Task<EmbeddingResponse> GenerateEmbeddingsAsync(
        string modelId,
        IReadOnlyList<string> texts,
        bool normalize = false,
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return _depot.GenerateEmbeddingsAsync(
            modelId,
            texts,
            normalize,
            parameters,
            cancellationToken);
    }

    #endregion

    /// <summary>
    /// Disposes resources
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        // Dispose depot
        if (_depot is IDisposable disposableDepot)
        {
            disposableDepot.Dispose();
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

/// <summary>
/// Forwarding logger provider that uses a single logger
/// </summary>
internal class ForwardingLoggerProvider : ILoggerProvider
{
    private readonly ILogger _logger;

    public ForwardingLoggerProvider(ILogger logger)
    {
        _logger = logger;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return _logger;
    }

    public void Dispose()
    {
    }
}