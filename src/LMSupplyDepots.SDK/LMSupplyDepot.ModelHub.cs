using LMSupplyDepots.Models;
using LMSupplyDepots.ModelHub.Models;
using LMSupplyDepots.ModelHub.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using LMSupplyDepots.ModelHub;
using LMSupplyDepots.ModelHub.HuggingFace;

namespace LMSupplyDepots.SDK;

/// <summary>
/// Model management functionality for LMSupplyDepot
/// </summary>
public partial class LMSupplyDepot
{
    /// <summary>
    /// Gets the model manager that provides model management capabilities
    /// </summary>
    private IModelManager ModelManager => _serviceProvider.GetRequiredService<IModelManager>();

    /// <summary>
    /// Lists available models with optional filtering
    /// </summary>
    public Task<IReadOnlyList<LMModel>> ListModelsAsync(
        ModelType? type = null,
        string? searchTerm = null,
        CancellationToken cancellationToken = default)
    {
        return ModelManager.ListModelsAsync(type, searchTerm, 0, 1000, cancellationToken);
    }

    /// <summary>
    /// Searches for models from external sources
    /// </summary>
    public async Task<IReadOnlyList<ModelSearchResult>> SearchModelsAsync(
        ModelType? type = null,
        string? searchTerm = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        // Search for repositories
        var repos = await ModelManager.SearchRepositoriesAsync(type, searchTerm, limit, cancellationToken);

        // Convert to ModelSearchResult objects 
        var results = new List<ModelSearchResult>();
        foreach (var repo in repos)
        {
            // Get a recommended model from each repository
            var model = repo.GetRecommendedModel();
            if (model != null)
            {
                var downloadStatus = ModelManager.GetDownloadStatus(model.Id);

                results.Add(new ModelSearchResult
                {
                    Model = model,
                    SourceName = model.Registry,
                    SourceId = model.Id,
                    IsDownloaded = await ModelManager.IsModelDownloadedAsync(model.Id, cancellationToken),
                    IsDownloading = downloadStatus == ModelDownloadStatus.Downloading,
                    IsPaused = downloadStatus == ModelDownloadStatus.Paused
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Downloads a model from an external source
    /// </summary>
    public Task<LMModel> DownloadModelAsync(
        string modelKey,
        IProgress<ModelDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return ModelManager.DownloadModelAsync(modelKey, progress, cancellationToken);
    }

    /// <summary>
    /// Pauses an active model download
    /// </summary>
    public async Task<bool> PauseDownloadAsync(
        string modelKey,
        CancellationToken cancellationToken = default)
    {
        string modelId = await ModelManager.ResolveModelKeyAsync(modelKey, cancellationToken);
        return await ModelManager.PauseDownloadAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Resumes a paused model download
    /// </summary>
    public async Task<ModelDownloadState> ResumeDownloadAsync(
        string modelKey,
        IProgress<ModelDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        string modelId = await ModelManager.ResolveModelKeyAsync(modelKey, cancellationToken);
        return await ModelManager.ResumeDownloadAsync(modelId, progress, cancellationToken);
    }

    /// <summary>
    /// Cancels an active or paused model download
    /// </summary>
    public async Task<bool> CancelDownloadAsync(
        string modelKey,
        CancellationToken cancellationToken = default)
    {
        string modelId = await ModelManager.ResolveModelKeyAsync(modelKey, cancellationToken);
        return await ModelManager.CancelDownloadAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Gets the current status of a model download
    /// </summary>
    public async Task<ModelDownloadStatus?> GetDownloadStatusAsync(string modelKey, CancellationToken cancellationToken = default)
    {
        string modelId = await ModelManager.ResolveModelKeyAsync(modelKey, cancellationToken);
        return ModelManager.GetDownloadStatus(modelId);
    }

    /// <summary>
    /// Gets information about all active downloads
    /// </summary>
    public IReadOnlyDictionary<string, ModelDownloadState> GetActiveDownloads()
    {
        return ModelManager.GetActiveDownloads();
    }

    /// <summary>
    /// Gets information about a model from an external source without downloading it
    /// </summary>
    public Task<LMModel> GetModelInfoAsync(
        string modelKey,
        CancellationToken cancellationToken = default)
    {
        return ModelManager.GetExternalModelInfoAsync(modelKey, cancellationToken);
    }

    /// <summary>
    /// Gets information about a model repository from an external source
    /// </summary>
    public Task<LMRepo> GetRepositoryInfoAsync(
        string repoKey,
        CancellationToken cancellationToken = default)
    {
        return ModelManager.GetRepositoryInfoAsync(repoKey, cancellationToken);
    }

    /// <summary>
    /// Gets a model by its ID or alias
    /// </summary>
    public async Task<LMModel?> GetModelAsync(
        string modelKey,
        CancellationToken cancellationToken = default)
    {
        // First try by ID
        var model = await ModelManager.GetModelAsync(modelKey, cancellationToken);
        if (model != null)
        {
            return model;
        }

        // Then try by alias
        return await ModelManager.GetModelByAliasAsync(modelKey, cancellationToken);
    }

    /// <summary>
    /// Deletes a model
    /// </summary>
    public async Task<bool> DeleteModelAsync(
        string modelKey,
        CancellationToken cancellationToken = default)
    {
        var resolvedId = await ModelManager.ResolveModelKeyAsync(modelKey, cancellationToken);
        return await ModelManager.DeleteModelAsync(resolvedId, cancellationToken);
    }

    /// <summary>
    /// Sets an alias for a model
    /// </summary>
    public async Task<LMModel> SetModelAliasAsync(
        string modelKey,
        string? alias,
        CancellationToken cancellationToken = default)
    {
        var resolvedId = await ModelManager.ResolveModelKeyAsync(modelKey, cancellationToken);
        return await ModelManager.SetModelAliasAsync(resolvedId, alias, cancellationToken);
    }

    /// <summary>
    /// Gets a model by its alias
    /// </summary>
    public async Task<LMModel?> GetModelByAliasAsync(
        string alias,
        CancellationToken cancellationToken = default)
    {
        return await ModelManager.GetModelByAliasAsync(alias, cancellationToken);
    }

    /// <summary>
    /// Configures ModelHub services
    /// </summary>
    private void ConfigureModelHubServices(IServiceCollection services, string modelsPath)
    {
        // Add ModelHub services
        services.AddModelHub(options =>
        {
            options.DataPath = modelsPath;
            options.MaxConcurrentDownloads = _options.MaxConcurrentDownloads;
            options.VerifyChecksums = _options.VerifyChecksums;
            options.MinimumFreeDiskSpace = _options.MinimumFreeDiskSpace;
        });

        // Add HuggingFace downloader
        services.AddHuggingFaceDownloader(options =>
        {
            options.ApiToken = _options.HuggingFaceApiToken;
            options.MaxConcurrentFileDownloads = _options.MaxConcurrentFileDownloads;
            options.RequestTimeout = _options.HttpRequestTimeout;
            options.MaxRetries = _options.HttpMaxRetries;
        });
    }
}