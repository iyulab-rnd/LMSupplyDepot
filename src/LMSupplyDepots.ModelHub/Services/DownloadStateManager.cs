using System.Text.Json;
using LMSupplyDepots.ModelHub.Utils;
using LMSupplyDepots.Utils;

namespace LMSupplyDepots.ModelHub.Services;

/// <summary>
/// Manages download state persistence and retrieval
/// </summary>
public class DownloadStateManager
{
    private readonly ILogger _logger;
    private readonly string _basePath;

    /// <summary>
    /// Initializes a new instance of the DownloadStateManager
    /// </summary>
    public DownloadStateManager(ILogger logger, string basePath)
    {
        _logger = logger;
        _basePath = basePath;

        // Ensure downloads directory exists
        var downloadsDir = Path.Combine(basePath, FileSystemHelper.DownloadsDirectory);
        Directory.CreateDirectory(downloadsDir);
    }

    /// <summary>
    /// Saves a download state to disk
    /// </summary>
    public async Task SaveStateAsync(ModelDownloadState state, CancellationToken cancellationToken = default)
    {
        try
        {
            // Create a clean copy without the CancellationTokenSource
            var stateCopy = new ModelDownloadState
            {
                ModelId = state.ModelId,
                TargetDirectory = state.TargetDirectory,
                ModelType = state.ModelType,
                Status = state.Status,
                StartTime = state.StartTime,
                LastUpdateTime = state.LastUpdateTime,
                TotalBytes = state.TotalBytes,
                BytesDownloaded = state.BytesDownloaded,
                DownloadedFiles = state.DownloadedFiles,
                ProviderData = state.ProviderData,
                Message = state.Message
            };

            var filePath = GetStateFilePath(state.ModelId);
            var json = JsonHelper.Serialize(stateCopy);

            await File.WriteAllTextAsync(filePath, json, cancellationToken);
            _logger.LogDebug("Saved download state for {ModelId}", state.ModelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving download state for {ModelId}", state.ModelId);
        }
    }

    /// <summary>
    /// Loads a download state from disk
    /// </summary>
    public async Task<ModelDownloadState?> LoadStateAsync(string modelId, CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = GetStateFilePath(modelId);
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var state = JsonHelper.Deserialize<ModelDownloadState>(json);

            if (state != null)
            {
                // Initialize with a new cancellation token source
                state.CancellationTokenSource = new CancellationTokenSource();
                state.Status = ModelDownloadStatus.Paused; // Default to paused when loaded
                _logger.LogDebug("Loaded download state for {ModelId}", modelId);
                return state;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading download state for {ModelId}", modelId);
        }

        return null;
    }

    /// <summary>
    /// Deletes a download state from disk
    /// </summary>
    public bool DeleteState(string modelId)
    {
        try
        {
            var filePath = GetStateFilePath(modelId);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                _logger.LogDebug("Deleted download state for {ModelId}", modelId);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting download state for {ModelId}", modelId);
        }

        return false;
    }

    /// <summary>
    /// Loads all existing download states
    /// </summary>
    public async Task<Dictionary<string, ModelDownloadState>> LoadAllStatesAsync(CancellationToken cancellationToken = default)
    {
        var result = new Dictionary<string, ModelDownloadState>();
        try
        {
            var downloadsDir = Path.Combine(_basePath, FileSystemHelper.DownloadsDirectory);
            if (!Directory.Exists(downloadsDir))
            {
                return result;
            }

            var files = Directory.GetFiles(downloadsDir, $"*{FileSystemHelper.DownloadStatusFileExtension}");
            foreach (var file in files)
            {
                try
                {
                    var modelId = Path.GetFileNameWithoutExtension(file);
                    var state = await LoadStateAsync(modelId, cancellationToken);
                    if (state != null)
                    {
                        result.Add(modelId, state);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error loading download state from {FilePath}", file);
                }
            }

            _logger.LogInformation("Loaded {Count} download states", result.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading download states");
        }

        return result;
    }

    /// <summary>
    /// Gets the file path for a download state file
    /// </summary>
    private string GetStateFilePath(string modelId)
    {
        var downloadsDir = Path.Combine(_basePath, FileSystemHelper.DownloadsDirectory);
        var safeFileName = modelId.Replace(':', '_').Replace('/', '_');
        return Path.Combine(downloadsDir, $"{safeFileName}{FileSystemHelper.DownloadStatusFileExtension}");
    }
}