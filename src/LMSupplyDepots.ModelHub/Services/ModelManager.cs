using LMSupplyDepots.Interfaces;
using LMSupplyDepots.ModelHub.Repositories;
using LMSupplyDepots.Utils;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace LMSupplyDepots.ModelHub.Services;

/// <summary>
/// Implementation of IModelManager that coordinates model operations.
/// </summary>
public class ModelManager : IModelManager, IDisposable
{
    private readonly ModelHubOptions _options;
    private readonly ILogger<ModelManager> _logger;
    private readonly IModelRepository _repository;
    private readonly FileSystemModelRepository _fileSystemRepository;
    private readonly IEnumerable<IModelDownloader> _downloaders;
    private readonly DownloadManager _downloadManager;
    private readonly ConcurrentDictionary<string, Task<LMModel>> _activeDownloads = new();
    private readonly ConcurrentDictionary<string, LMRepo> _repoCache = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    public ModelManager(
        IOptions<ModelHubOptions> options,
        ILogger<ModelManager> logger,
        IModelRepository repository,
        FileSystemModelRepository fileSystemRepository,
        IEnumerable<IModelDownloader> downloaders,
        DownloadManager downloadManager)
    {
        _options = options.Value;
        _logger = logger;
        _repository = repository;
        _fileSystemRepository = fileSystemRepository;
        _downloaders = downloaders;
        _downloadManager = downloadManager;
    }

    /// <summary>
    /// Gets a model by its identifier.
    /// </summary>
    public Task<LMModel?> GetModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return _repository.GetModelAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Checks if a model is downloaded.
    /// </summary>
    public async Task<bool> IsModelDownloadedAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return await _repository.ExistsAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Lists models with optional filtering and pagination.
    /// </summary>
    public Task<IReadOnlyList<LMModel>> ListModelsAsync(
        ModelType? type = null,
        string? searchTerm = null,
        int skip = 0,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        return _repository.ListModelsAsync(type, searchTerm, skip, take, cancellationToken);
    }

    /// <summary>
    /// Searches for model repositories in external sources.
    /// </summary>
    public async Task<IReadOnlyList<LMRepo>> SearchRepositoriesAsync(
        ModelType? type = null,
        string? searchTerm = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching for repositories with term: {SearchTerm}, type: {Type}, limit: {Limit}",
            searchTerm, type, limit);

        var results = new List<LMRepo>();
        var tasks = new List<Task<IReadOnlyList<LMRepo>>>();

        // For each downloader, search for repositories
        foreach (var downloader in _downloaders)
        {
            tasks.Add(SearchSourceAsync(downloader, type, searchTerm, limit, cancellationToken));
        }

        // Wait for all searches to complete
        await Task.WhenAll(tasks);

        // Combine results
        foreach (var task in tasks)
        {
            try
            {
                var sourceResults = await task;
                results.AddRange(sourceResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting repository search results");
            }
        }

        // Return results sorted by name, limited to the requested count
        return results
            .GroupBy(r => r.Id)
            .Select(g => g.First()) // Remove duplicates
            .OrderBy(r => r.Name)
            .Take(limit)
            .ToList();
    }

    /// <summary>
    /// Gets information about a model repository.
    /// </summary>
    public async Task<LMRepo> GetRepositoryInfoAsync(string repoId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting repository information for {RepoId}", repoId);

        // Check if we have it in cache
        if (_repoCache.TryGetValue(repoId, out var cachedRepo))
        {
            return cachedRepo;
        }

        // Find a downloader that can handle this repository
        var downloader = GetDownloaderForRepo(repoId);
        if (downloader == null)
        {
            throw new InvalidOperationException($"No downloader can handle repository ID: {repoId}");
        }

        // Get repository information
        var repo = await downloader.GetRepositoryInfoAsync(repoId, cancellationToken);

        // Cache the result
        _repoCache[repoId] = repo;

        return repo;
    }

    /// <summary>
    /// Gets information about a model from an external source without downloading it.
    /// </summary>
    public async Task<LMModel> GetExternalModelInfoAsync(
        string modelId,
        CancellationToken cancellationToken = default)
    {
        var downloader = GetDownloader(modelId);
        if (downloader == null)
        {
            throw new InvalidOperationException($"No downloader can handle model ID: {modelId}");
        }

        return await downloader.GetModelInfoAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Downloads a model from an external source.
    /// </summary>
    public async Task<LMModel> DownloadModelAsync(
        string modelId,
        IProgress<ModelDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Check if there's an existing download task
        if (_activeDownloads.TryGetValue(modelId, out var existingTask))
        {
            _logger.LogInformation("Model {ModelId} is already being downloaded, returning existing task", modelId);
            return await existingTask;
        }

        // Check if the model is already in the repository
        var existingModel = await _repository.GetModelAsync(modelId, cancellationToken);
        if (existingModel != null)
        {
            _logger.LogInformation("Model {ModelId} already exists in the repository", modelId);
            return existingModel;
        }

        // Find a suitable downloader
        var downloader = GetDownloader(modelId);
        if (downloader == null)
        {
            throw new InvalidOperationException($"No downloader can handle model ID: {modelId}");
        }

        // Get model info
        var modelInfo = await downloader.GetModelInfoAsync(modelId, cancellationToken);

        // Create target directory
        var targetDirectory = _fileSystemRepository.GetModelDirectoryPath(modelInfo.Id, modelInfo.Type);

        // Create a download task
        var downloadTask = StartDownloadAsync(downloader, modelId, modelInfo.Type, targetDirectory, progress, cancellationToken);

        // Register the task
        _activeDownloads[modelId] = downloadTask;

        try
        {
            // Wait for download to complete
            return await downloadTask;
        }
        finally
        {
            // Remove the task from active downloads
            _activeDownloads.TryRemove(modelId, out _);
        }
    }

    /// <summary>
    /// Pauses a model download.
    /// </summary>
    public async Task<bool> PauseDownloadAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return await _downloadManager.PauseDownloadAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Resumes a paused model download.
    /// </summary>
    public async Task<ModelDownloadState> ResumeDownloadAsync(
        string modelId,
        IProgress<ModelDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return await _downloadManager.ResumeDownloadAsync(modelId, progress, cancellationToken);
    }

    /// <summary>
    /// Cancels a model download.
    /// </summary>
    public async Task<bool> CancelDownloadAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return await _downloadManager.CancelDownloadAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Gets the status of a model download.
    /// </summary>
    public ModelDownloadStatus? GetDownloadStatus(string modelId)
    {
        return _downloadManager.GetDownloadStatus(modelId);
    }

    /// <summary>
    /// Gets information about all active downloads.
    /// </summary>
    public IReadOnlyDictionary<string, ModelDownloadState> GetActiveDownloads()
    {
        return _downloadManager.ActiveDownloads;
    }

    /// <summary>
    /// Deletes a model from the local repository.
    /// </summary>
    public Task<bool> DeleteModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        // Check if the model is currently being downloaded
        if (_downloadManager.IsDownloading(modelId) || _downloadManager.IsPaused(modelId))
        {
            // Cancel the download first
            _downloadManager.CancelDownloadAsync(modelId, cancellationToken).Wait(cancellationToken);
        }

        return _repository.DeleteModelAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Searches for repositories using a specific downloader
    /// </summary>
    private async Task<IReadOnlyList<LMRepo>> SearchSourceAsync(
        IModelDownloader downloader,
        ModelType? type,
        string? searchTerm,
        int limit,
        CancellationToken cancellationToken)
    {
        try
        {
            var repos = await downloader.SearchRepositoriesAsync(type, searchTerm, limit, cancellationToken);

            // Cache the repositories
            foreach (var repo in repos)
            {
                _repoCache[repo.Id] = repo;
            }

            return repos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching repositories from {Source}", downloader.SourceName);
            return Array.Empty<LMRepo>();
        }
    }

    /// <summary>
    /// Gets a downloader that can handle the given model ID
    /// </summary>
    private IModelDownloader? GetDownloader(string modelId)
    {
        return _downloaders.FirstOrDefault(d => d.CanHandle(modelId));
    }

    /// <summary>
    /// Gets a downloader that can handle the given repository ID
    /// </summary>
    private IModelDownloader? GetDownloaderForRepo(string repoId)
    {
        // For repositories, we treat them similar to models but without artifacts
        string modelId;

        // Check if it already includes the registry prefix
        if (repoId.Contains(':'))
        {
            modelId = repoId;
        }
        else
        {
            // Assume it's a HuggingFace repo if no registry specified
            modelId = $"hf:{repoId}";
        }

        return GetDownloader(modelId);
    }

    /// <summary>
    /// Starts the download process for a model
    /// </summary>
    private async Task<LMModel> StartDownloadAsync(
        IModelDownloader downloader,
        string modelId,
        ModelType modelType,
        string targetDirectory,
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken cancellationToken)
    {
        // Start the download using the download manager
        var downloadState = await _downloadManager.StartDownloadAsync(
            modelId,
            modelType,
            targetDirectory,
            progress,
            cancellationToken);

        // We need to wait until the download is completed or failed
        var complete = false;
        LMModel? result = null;
        Exception? error = null;

        // Create a timer to check the download status
        using var timer = new System.Timers.Timer(500); // Check every 500ms
        using var waitHandle = new ManualResetEvent(false);

        timer.Elapsed += async (sender, e) =>
        {
            try
            {
                // Get the current download state
                var state = _downloadManager.GetDownloadState(modelId);

                if (state == null ||
                    state.Status == ModelDownloadStatus.Completed ||
                    state.Status == ModelDownloadStatus.Failed ||
                    state.Status == ModelDownloadStatus.Cancelled)
                {
                    // Download is no longer active
                    if (state?.Status == ModelDownloadStatus.Completed)
                    {
                        // If completed, try to get the model from the repository
                        result = await _repository.GetModelAsync(modelId, cancellationToken);

                        // If not found in repository, try to get from disk
                        if (result == null)
                        {
                            // Load the model from the downloaded files
                            try
                            {
                                // Find the model file
                                var modelFiles = Directory.GetFiles(targetDirectory, "*.gguf");
                                if (modelFiles.Length > 0)
                                {
                                    var mainModelFile = modelFiles.OrderByDescending(f => new FileInfo(f).Length).First();
                                    var jsonFile = Path.ChangeExtension(mainModelFile, ".json");

                                    if (File.Exists(jsonFile))
                                    {
                                        // The model was successfully downloaded and metadata was created
                                        var json = await File.ReadAllTextAsync(jsonFile, cancellationToken);
                                        result = JsonHelper.Deserialize<LMModel>(json);

                                        if (result != null)
                                        {
                                            // Save to repository to make sure it's properly registered
                                            result = await _repository.SaveModelAsync(result, cancellationToken);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error loading model files for {ModelId}", modelId);
                                error = ex;
                            }
                        }
                    }
                    else if (state?.Status == ModelDownloadStatus.Failed)
                    {
                        string errorMessage = state.Message ?? "Unknown error";

                        // Check for authentication errors and provide more helpful message
                        if (errorMessage.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
                            errorMessage.Contains("unauthorized", StringComparison.OrdinalIgnoreCase) ||
                            errorMessage.Contains("API token", StringComparison.OrdinalIgnoreCase))
                        {
                            error = new ModelDownloadException(
                                modelId,
                                "This model requires authentication. Please provide a valid API token in the settings.");
                        }
                        else
                        {
                            error = new ModelDownloadException(modelId, errorMessage);
                        }
                    }
                    else if (state?.Status == ModelDownloadStatus.Cancelled)
                    {
                        error = new OperationCanceledException($"Download cancelled for model {modelId}");
                    }

                    complete = true;
                    waitHandle.Set();
                    timer.Stop();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking download status for {ModelId}", modelId);
                error = ex;
                complete = true;
                waitHandle.Set();
                timer.Stop();
            }
        };

        // Start the timer
        timer.Start();

        // Wait for the download to complete
        await Task.Run(() => waitHandle.WaitOne(), cancellationToken);

        // Check the result
        if (error != null)
        {
            throw error;
        }

        if (result == null)
        {
            throw new InvalidOperationException($"Failed to download or load model {modelId}");
        }

        return result;
    }

    /// <summary>
    /// Sets an alias for a model
    /// </summary>
    public async Task<LMModel> SetModelAliasAsync(
        string modelId,
        string? alias,
        CancellationToken cancellationToken = default)
    {
        // Get the model first
        var model = await GetModelAsync(modelId, cancellationToken);
        if (model == null)
        {
            throw new ModelNotFoundException(modelId, "Model not found");
        }

        // If alias is null or empty, clear it
        if (string.IsNullOrEmpty(alias))
        {
            _logger.LogInformation("Clearing alias for model {ModelId}", modelId);
            model.Alias = null;
        }
        else
        {
            // Check if the alias is already used by another model
            var modelWithSameAlias = await GetModelByAliasAsync(alias, cancellationToken);
            if (modelWithSameAlias != null && modelWithSameAlias.Id != modelId)
            {
                throw new InvalidOperationException(
                    $"Alias '{alias}' is already used by model '{modelWithSameAlias.Id}'");
            }

            _logger.LogInformation("Setting alias '{Alias}' for model {ModelId}", alias, modelId);
            model.Alias = alias;
        }

        // Save the updated model
        return await _repository.SaveModelAsync(model, cancellationToken);
    }

    /// <summary>
    /// Gets a model by its alias
    /// </summary>
    public async Task<LMModel?> GetModelByAliasAsync(
        string alias,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(alias))
        {
            return null;
        }

        // List all models and find the one with matching alias
        var allModels = await _repository.ListModelsAsync(
            null, null, 0, int.MaxValue, cancellationToken);

        return allModels.FirstOrDefault(m =>
            !string.IsNullOrEmpty(m.Alias) &&
            string.Equals(m.Alias, alias, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                // Cancel all active downloads
                foreach (var modelId in _activeDownloads.Keys)
                {
                    _downloadManager.CancelDownloadAsync(modelId).Wait();
                }

                _activeDownloads.Clear();
                _repoCache.Clear();
            }

            _disposed = true;
        }
    }
}