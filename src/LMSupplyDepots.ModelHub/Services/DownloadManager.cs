using System.Collections.Concurrent;
using LMSupplyDepots.External.HuggingFace.Client;
using System.Net;
using LMSupplyDepots.ModelHub.Exceptions;
using Microsoft.Extensions.Options;
using LMSupplyDepots.ModelHub.HuggingFace;
using LMSupplyDepots.ModelHub.Utils;

namespace LMSupplyDepots.ModelHub.Services;

/// <summary>
/// Manages model download operations
/// </summary>
public class DownloadManager : IDisposable
{
    private readonly ILogger<DownloadManager> _logger;
    private readonly ModelHubOptions _options;
    private readonly IEnumerable<IModelDownloader> _downloaders;
    private readonly DownloadStateManager _stateManager;
    private readonly ConcurrentDictionary<string, ModelDownloadState> _activeDownloads = new();
    private readonly SemaphoreSlim _downloadSemaphore;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the DownloadManager
    /// </summary>
    public DownloadManager(
        ILogger<DownloadManager> logger,
        IOptions<ModelHubOptions> options,
        IEnumerable<IModelDownloader> downloaders)
    {
        _logger = logger;
        _options = options.Value;
        _downloaders = downloaders;
        _downloadSemaphore = new SemaphoreSlim(_options.MaxConcurrentDownloads, _options.MaxConcurrentDownloads);
        _stateManager = new DownloadStateManager(_logger, _options.DataPath);

        // Load any existing download states
        LoadDownloadStatesAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets all active downloads
    /// </summary>
    public IReadOnlyDictionary<string, ModelDownloadState> ActiveDownloads =>
        _activeDownloads as IReadOnlyDictionary<string, ModelDownloadState>;

    /// <summary>
    /// Starts a new download
    /// </summary>
    public async Task<ModelDownloadState> StartDownloadAsync(
        string sourceId,
        ModelType modelType,
        string targetDirectory,
        IProgress<ModelDownloadProgress>? progress = null,
        CancellationToken externalCancellationToken = default)
    {
        // Check if the download is already in progress
        if (_activeDownloads.TryGetValue(sourceId, out var existingState))
        {
            if (existingState.Status == ModelDownloadStatus.Downloading)
            {
                _logger.LogInformation("Download already in progress for model {ModelId}", sourceId);
                return existingState;
            }
            else if (existingState.Status == ModelDownloadStatus.Paused)
            {
                // Resume the download
                return await ResumeDownloadAsync(sourceId, progress, externalCancellationToken);
            }
        }

        // Find a suitable downloader
        var downloader = GetDownloader(sourceId);

        // Create download state
        var downloadState = new ModelDownloadState
        {
            ModelId = sourceId,
            TargetDirectory = targetDirectory,
            ModelType = modelType,
            Status = ModelDownloadStatus.Initializing,
            CancellationTokenSource = new CancellationTokenSource(),
            DownloadedFiles = new Dictionary<string, long>(),
            ProviderData = new Dictionary<string, string>()
        };

        // Register the download
        _activeDownloads[sourceId] = downloadState;

        // Save download state
        await _stateManager.SaveStateAsync(downloadState);

        // Start the download in a background task
        _ = Task.Run(async () =>
        {
            await _downloadSemaphore.WaitAsync(CancellationToken.None);

            try
            {
                await ProcessDownloadAsync(downloader, downloadState, progress, externalCancellationToken);
            }
            finally
            {
                _downloadSemaphore.Release();
            }
        });

        return downloadState;
    }

    /// <summary>
    /// Pauses an active download
    /// </summary>
    public async Task<bool> PauseDownloadAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        if (!_activeDownloads.TryGetValue(sourceId, out var downloadState))
        {
            _logger.LogWarning("Cannot pause download for model {ModelId} - not found", sourceId);
            return false;
        }

        if (downloadState.Status != ModelDownloadStatus.Downloading)
        {
            _logger.LogWarning("Cannot pause download for model {ModelId} - not in downloading state", sourceId);
            return false;
        }

        try
        {
            // Find the downloader
            var downloader = GetDownloader(sourceId);

            // Pause at the downloader level first
            var paused = await downloader.PauseDownloadAsync(sourceId, cancellationToken);
            if (!paused)
            {
                _logger.LogWarning("Downloader failed to pause download for model {ModelId}", sourceId);
                return false;
            }

            // Cancel the current download task (without disposing the token source)
            downloadState.CancellationTokenSource?.Cancel();

            // Update status
            downloadState.Status = ModelDownloadStatus.Paused;
            downloadState.LastUpdateTime = DateTime.UtcNow;

            // Save the updated state
            await _stateManager.SaveStateAsync(downloadState);

            _logger.LogInformation("Download paused for model {ModelId}", sourceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing download for model {ModelId}", sourceId);
            return false;
        }
    }

    /// <summary>
    /// Resumes a paused download
    /// </summary>
    public async Task<ModelDownloadState> ResumeDownloadAsync(
        string sourceId,
        IProgress<ModelDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!_activeDownloads.TryGetValue(sourceId, out var downloadState))
        {
            _logger.LogWarning("Cannot resume download for model {ModelId} - not found", sourceId);
            throw new InvalidOperationException($"No paused download found for model {sourceId}");
        }

        if (downloadState.Status != ModelDownloadStatus.Paused)
        {
            _logger.LogWarning("Cannot resume download for model {ModelId} - not in paused state", sourceId);
            throw new InvalidOperationException($"Download for model {sourceId} is not paused");
        }

        // Find the downloader
        var downloader = GetDownloader(sourceId);

        // Create a new cancellation token source
        downloadState.CancellationTokenSource = new CancellationTokenSource();

        // Start the download in a background task
        _ = Task.Run(async () =>
        {
            await _downloadSemaphore.WaitAsync(CancellationToken.None);

            try
            {
                await ProcessResumeAsync(downloader, downloadState, progress, cancellationToken);
            }
            finally
            {
                _downloadSemaphore.Release();
            }
        });

        return downloadState;
    }

    /// <summary>
    /// Cancels an active or paused download
    /// </summary>
    public async Task<bool> CancelDownloadAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        if (!_activeDownloads.TryGetValue(sourceId, out var downloadState))
        {
            _logger.LogWarning("Cannot cancel download for model {ModelId} - not found", sourceId);
            return false;
        }

        if (downloadState.Status != ModelDownloadStatus.Downloading &&
            downloadState.Status != ModelDownloadStatus.Paused)
        {
            _logger.LogWarning("Cannot cancel download for model {ModelId} - not in downloading or paused state", sourceId);
            return false;
        }

        try
        {
            // Find the downloader
            var downloader = GetDownloader(sourceId);

            // Cancel at the downloader level first
            var cancelled = await downloader.CancelDownloadAsync(sourceId, cancellationToken);

            // Cancel the current download task and dispose the token source
            downloadState.CancellationTokenSource?.Cancel();
            downloadState.CancellationTokenSource?.Dispose();
            downloadState.CancellationTokenSource = null;

            // Update status
            downloadState.Status = ModelDownloadStatus.Cancelled;
            downloadState.LastUpdateTime = DateTime.UtcNow;

            // Save the updated state (temporarily, will be deleted below)
            await _stateManager.SaveStateAsync(downloadState);

            // Remove from active downloads
            _activeDownloads.TryRemove(sourceId, out _);

            // Delete the download state file
            _stateManager.DeleteState(sourceId);

            _logger.LogInformation("Download cancelled for model {ModelId}", sourceId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling download for model {ModelId}", sourceId);
            return false;
        }
    }

    /// <summary>
    /// Gets the download state for a model
    /// </summary>
    public ModelDownloadState? GetDownloadState(string sourceId)
    {
        _activeDownloads.TryGetValue(sourceId, out var downloadState);
        return downloadState;
    }

    /// <summary>
    /// Gets the download status for a model
    /// </summary>
    public ModelDownloadStatus? GetDownloadStatus(string sourceId)
    {
        if (_activeDownloads.TryGetValue(sourceId, out var downloadState))
        {
            return downloadState.Status;
        }

        return null;
    }

    /// <summary>
    /// Checks if a model is currently being downloaded
    /// </summary>
    public bool IsDownloading(string sourceId)
    {
        if (_activeDownloads.TryGetValue(sourceId, out var downloadState))
        {
            return downloadState.Status == ModelDownloadStatus.Downloading;
        }

        return false;
    }

    /// <summary>
    /// Checks if a model download is paused
    /// </summary>
    public bool IsPaused(string sourceId)
    {
        if (_activeDownloads.TryGetValue(sourceId, out var downloadState))
        {
            return downloadState.Status == ModelDownloadStatus.Paused;
        }

        return false;
    }

    /// <summary>
    /// Loads download states from disk
    /// </summary>
    private async Task LoadDownloadStatesAsync()
    {
        try
        {
            var states = await _stateManager.LoadAllStatesAsync();
            foreach (var state in states)
            {
                _activeDownloads[state.Key] = state.Value;
            }

            _logger.LogInformation("Loaded {Count} download states", _activeDownloads.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading download states");
        }
    }

    /// <summary>
    /// Processes a download
    /// </summary>
    private async Task ProcessDownloadAsync(
        IModelDownloader downloader,
        ModelDownloadState downloadState,
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken externalCancellationToken)
    {
        // Link the external cancellation token to our internal one
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            externalCancellationToken, downloadState.CancellationTokenSource!.Token);
        var cancellationToken = linkedCts.Token;

        // Update status to downloading
        downloadState.Status = ModelDownloadStatus.Downloading;
        await _stateManager.SaveStateAsync(downloadState);

        // Create a progress tracker that updates our download state
        var progressTracker = new Progress<ModelDownloadProgress>(p =>
        {
            downloadState.BytesDownloaded = p.BytesDownloaded;
            if (!downloadState.TotalBytes.HasValue || p.TotalBytes.HasValue)
            {
                downloadState.TotalBytes = p.TotalBytes;
            }
            downloadState.LastUpdateTime = DateTime.UtcNow;

            // Update download state file
            _stateManager.SaveStateAsync(downloadState).ConfigureAwait(false);

            // Forward progress to the caller's progress reporter
            progress?.Report(p);
        });

        // Start the download
        try
        {
            _logger.LogInformation("Starting download of model {ModelId}", downloadState.ModelId);
            await downloader.DownloadModelAsync(
                downloadState.ModelId,
                downloadState.TargetDirectory,
                progressTracker,
                cancellationToken);

            // Download completed successfully
            downloadState.Status = ModelDownloadStatus.Completed;
            _logger.LogInformation("Download completed for model {ModelId}", downloadState.ModelId);
        }
        catch (OperationCanceledException)
        {
            // Check if cancellation was requested by us or externally
            if (downloadState.CancellationTokenSource!.IsCancellationRequested)
            {
                downloadState.Status = ModelDownloadStatus.Cancelled;
                _logger.LogInformation("Download cancelled for model {ModelId}", downloadState.ModelId);
            }
            else
            {
                downloadState.Status = ModelDownloadStatus.Paused;
                _logger.LogInformation("Download paused for model {ModelId}", downloadState.ModelId);
            }
        }
        catch (ModelDownloadException ex)
        {
            downloadState.Status = ModelDownloadStatus.Failed;

            // Use the model-specific error message
            downloadState.Message = ex.Message;

            // For authentication errors, provide a more user-friendly message
            if (ex.InnerException is HuggingFaceException hfEx && hfEx.StatusCode == HttpStatusCode.Unauthorized)
            {
                downloadState.Message = $"Authentication error: {ex.Message}";
                _logger.LogError("Authentication error downloading model {ModelId}: {Message}",
                    downloadState.ModelId, ex.Message);
            }
            else
            {
                _logger.LogError(ex, "Download failed for model {ModelId}", downloadState.ModelId);
            }
        }
        catch (Exception ex)
        {
            downloadState.Status = ModelDownloadStatus.Failed;
            downloadState.Message = ex.Message;
            _logger.LogError(ex, "Download failed for model {ModelId}", downloadState.ModelId);
        }

        // Always update the state file
        await _stateManager.SaveStateAsync(downloadState);

        // If completed or cancelled, remove from active downloads
        if (downloadState.Status == ModelDownloadStatus.Completed ||
            downloadState.Status == ModelDownloadStatus.Cancelled)
        {
            _activeDownloads.TryRemove(downloadState.ModelId, out _);
            _stateManager.DeleteState(downloadState.ModelId);
        }
    }

    /// <summary>
    /// Processes a resumed download
    /// </summary>
    private async Task ProcessResumeAsync(
        IModelDownloader downloader,
        ModelDownloadState downloadState,
        IProgress<ModelDownloadProgress>? progress,
        CancellationToken externalCancellationToken)
    {
        // Link the external cancellation token to our internal one
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            externalCancellationToken, downloadState.CancellationTokenSource!.Token);
        var cancellationToken = linkedCts.Token;

        // Update status to downloading
        downloadState.Status = ModelDownloadStatus.Downloading;
        await _stateManager.SaveStateAsync(downloadState);

        // Create a progress tracker
        var progressTracker = new Progress<ModelDownloadProgress>(p =>
        {
            downloadState.BytesDownloaded = p.BytesDownloaded;
            if (!downloadState.TotalBytes.HasValue || p.TotalBytes.HasValue)
            {
                downloadState.TotalBytes = p.TotalBytes;
            }
            downloadState.LastUpdateTime = DateTime.UtcNow;

            // Update download state file
            _stateManager.SaveStateAsync(downloadState).ConfigureAwait(false);

            // Forward progress to the caller's progress reporter
            progress?.Report(p);
        });

        // Resume the download
        try
        {
            _logger.LogInformation("Resuming download of model {ModelId}", downloadState.ModelId);
            await downloader.ResumeDownloadAsync(
                downloadState.ModelId,
                progressTracker,
                cancellationToken);

            // Download completed successfully
            downloadState.Status = ModelDownloadStatus.Completed;
            _logger.LogInformation("Download completed for model {ModelId}", downloadState.ModelId);
        }
        catch (OperationCanceledException)
        {
            // Check if cancellation was requested by us or externally
            if (downloadState.CancellationTokenSource!.IsCancellationRequested)
            {
                downloadState.Status = ModelDownloadStatus.Cancelled;
                _logger.LogInformation("Download cancelled for model {ModelId}", downloadState.ModelId);
            }
            else
            {
                downloadState.Status = ModelDownloadStatus.Paused;
                _logger.LogInformation("Download paused for model {ModelId}", downloadState.ModelId);
            }
        }
        catch (Exception ex)
        {
            downloadState.Status = ModelDownloadStatus.Failed;
            downloadState.Message = ex.Message;
            _logger.LogError(ex, "Download failed for model {ModelId}", downloadState.ModelId);
        }

        // Always update the state file
        await _stateManager.SaveStateAsync(downloadState);

        // If completed or cancelled, remove from active downloads
        if (downloadState.Status == ModelDownloadStatus.Completed ||
            downloadState.Status == ModelDownloadStatus.Cancelled)
        {
            _activeDownloads.TryRemove(downloadState.ModelId, out _);
            _stateManager.DeleteState(downloadState.ModelId);
        }
    }

    /// <summary>
    /// Gets a suitable downloader for a source ID
    /// </summary>
    private IModelDownloader GetDownloader(string sourceId)
    {
        var downloader = _downloaders.FirstOrDefault(d => d.CanHandle(sourceId));
        if (downloader == null)
        {
            throw new ModelSourceNotFoundException(sourceId);
        }
        return downloader;
    }

    /// <summary>
    /// Disposes resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _downloadSemaphore.Dispose();

                // Dispose all cancellation token sources
                foreach (var state in _activeDownloads.Values)
                {
                    state.CancellationTokenSource?.Dispose();
                }

                _activeDownloads.Clear();
            }

            _disposed = true;
        }
    }
}