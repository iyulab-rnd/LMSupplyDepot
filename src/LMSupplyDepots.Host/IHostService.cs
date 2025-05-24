using LMSupplyDepots.Models;
using LMSupplyDepots.ModelHub.Models;
using LMSupplyDepots.Contracts;

namespace LMSupplyDepots.Host;

/// <summary>
/// Interface for hosting LMSupplyDepots operations
/// </summary>
public interface IHostService
{
    #region Model Management

    /// <summary>
    /// Lists available models with optional filtering
    /// </summary>
    Task<IReadOnlyList<LMModel>> ListModelsAsync(ModelType? type = null, string? searchTerm = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a model by its ID
    /// </summary>
    Task<LMModel?> GetModelAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a model is loaded
    /// </summary>
    Task<bool> IsModelLoadedAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a list of loaded models
    /// </summary>
    Task<IReadOnlyList<LMModel>> GetLoadedModelsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a model
    /// </summary>
    Task<LMModel> LoadModelAsync(string modelId, Dictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a model
    /// </summary>
    Task UnloadModelAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a model
    /// </summary>
    Task<bool> DeleteModelAsync(string modelId, CancellationToken cancellationToken = default);

    #endregion

    #region Model Download and Repository

    /// <summary>
    /// Searches for models from external sources
    /// </summary>
    Task<IReadOnlyList<ModelSearchResult>> SearchModelsAsync(ModelType? type = null, string? searchTerm = null, int limit = 10, CancellationToken cancellationToken = default);

    /// <summary>
    /// Downloads a model from an external source
    /// </summary>
    Task<LMModel> DownloadModelAsync(string modelId, IProgress<ModelDownloadProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pauses an active model download
    /// </summary>
    Task<bool> PauseDownloadAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resumes a paused model download
    /// </summary>
    Task<ModelDownloadState> ResumeDownloadAsync(string modelId, IProgress<ModelDownloadProgress>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels an active or paused model download
    /// </summary>
    Task<bool> CancelDownloadAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a model download
    /// </summary>
    public Task<ModelDownloadStatus?> GetDownloadStatusAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about all active downloads
    /// </summary>
    IReadOnlyDictionary<string, ModelDownloadState> GetActiveDownloads();

    /// <summary>
    /// Gets information about a model from an external source without downloading it
    /// </summary>
    Task<LMModel> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets information about a model repository from an external source
    /// </summary>
    Task<LMRepo> GetRepositoryInfoAsync(string repoId, CancellationToken cancellationToken = default);

    #endregion

    #region Text Generation

    /// <summary>
    /// Generates text using a loaded model
    /// </summary>
    Task<GenerationResponse> GenerateTextAsync(string modelId, GenerationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates text using a loaded model with streaming output
    /// </summary>
    IAsyncEnumerable<string> GenerateTextStreamAsync(string modelId, GenerationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates text using a loaded model with simple parameters
    /// </summary>
    Task<GenerationResponse> GenerateTextAsync(
        string modelId,
        string prompt,
        int maxTokens = 256,
        float temperature = 0.7f,
        float topP = 0.95f,
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default);

    #endregion

    #region Embeddings

    /// <summary>
    /// Generates embeddings for texts using a loaded model
    /// </summary>
    Task<EmbeddingResponse> GenerateEmbeddingsAsync(
        string modelId,
        EmbeddingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates embeddings for texts using a loaded model with simple parameters
    /// </summary>
    Task<EmbeddingResponse> GenerateEmbeddingsAsync(
        string modelId,
        IReadOnlyList<string> texts,
        bool normalize = false,
        Dictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default);
    
    Task<LMModel> SetModelAliasAsync(string name, string alias, CancellationToken cancellationToken = default);

    #endregion
}