using LMSupplyDepots.ModelHub.Utils;
using LMSupplyDepots.Utils;
using System.Text.Json;

namespace LMSupplyDepots.ModelHub.HuggingFace;

/// <summary>
/// Implementation of download management operations
/// </summary>
public partial class HuggingFaceDownloader
{
    /// <summary>
    /// Pauses a download that is in progress
    /// </summary>
    public async Task<bool> PauseDownloadAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to pause download for model {ModelId}", sourceId);

        // Check if the download is active
        var downloadStatus = await GetDownloadStatusAsync(sourceId, cancellationToken);
        if (downloadStatus != ModelDownloadStatus.Downloading)
        {
            _logger.LogWarning("Cannot pause download for model {ModelId} - not in downloading state", sourceId);
            return false;
        }

        try
        {
            // Get the download directory path
            var normalizedId = HuggingFaceHelper.NormalizeSourceId(sourceId);
            var modelType = await HuggingFaceHelper.DetermineModelTypeAsync(normalizedId, _client.Value, cancellationToken);
            var targetDirectory = _fileSystemRepository.GetModelDirectoryPath(sourceId, modelType);

            // Create a paused download status file with current progress
            var pausedStatusFilePath = _fileSystemRepository.GetDownloadStatusFilePath(sourceId, modelType);

            // Check if we have an existing status file to determine progress
            long totalSize = 0;

            if (File.Exists(pausedStatusFilePath))
            {
                var content = await File.ReadAllTextAsync(pausedStatusFilePath, cancellationToken);
                if (long.TryParse(content.Trim(), out totalSize))
                {
                    // Update the status file to indicate it's paused
                    var statusInfo = new
                    {
                        ModelId = sourceId,
                        Status = "Paused",
                        PausedAt = DateTime.UtcNow,
                        TargetDirectory = targetDirectory,
                        TotalSize = totalSize,
                        // Calculate the downloaded size based on existing files
                        DownloadedSize = HuggingFaceHelper.CalculateDownloadedSize(targetDirectory)
                    };

                    var json = JsonHelper.Serialize(statusInfo);

                    await File.WriteAllTextAsync(pausedStatusFilePath, json, cancellationToken);
                    _logger.LogInformation("Download paused for model {ModelId}", sourceId);
                    return true;
                }
            }

            // Create a new status file with basic information
            var newStatusInfo = new
            {
                ModelId = sourceId,
                Status = "Paused",
                PausedAt = DateTime.UtcNow,
                TargetDirectory = targetDirectory,
                DownloadedSize = HuggingFaceHelper.CalculateDownloadedSize(targetDirectory)
            };

            var newJson = JsonHelper.Serialize(newStatusInfo);

            await File.WriteAllTextAsync(pausedStatusFilePath, newJson, cancellationToken);
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
    /// Cancels a download that is in progress or paused
    /// </summary>
    public async Task<bool> CancelDownloadAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to cancel download for model {ModelId}", sourceId);

        try
        {
            // Get the model type
            var normalizedId = HuggingFaceHelper.NormalizeSourceId(sourceId);
            var modelType = await HuggingFaceHelper.DetermineModelTypeAsync(normalizedId, _client.Value, cancellationToken);

            // Remove the status file if it exists
            var statusFilePath = _fileSystemRepository.GetDownloadStatusFilePath(sourceId, modelType);
            if (File.Exists(statusFilePath))
            {
                File.Delete(statusFilePath);
                _logger.LogInformation("Removed download status file for model {ModelId}", sourceId);
            }

            // Check if the directory was created and clean it up
            var targetDirectory = _fileSystemRepository.GetModelDirectoryPath(sourceId, modelType);
            if (Directory.Exists(targetDirectory))
            {
                var files = Directory.GetFiles(targetDirectory);
                if (files.Length == 0 ||
                    files.All(f => Path.GetExtension(f) == ".download" ||
                               Path.GetExtension(f) == ".part" ||
                               Path.GetExtension(f) == ".tmp"))
                {
                    // Only delete the directory if it only contains temporary files
                    try
                    {
                        Directory.Delete(targetDirectory, true);
                        _logger.LogInformation("Removed partial download directory for model {ModelId}", sourceId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete directory for model {ModelId}", sourceId);
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "Directory for model {ModelId} contains non-temporary files and will not be deleted",
                        sourceId);
                }
            }

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
    /// Gets the current status of a download
    /// </summary>
    public async Task<ModelDownloadStatus?> GetDownloadStatusAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedId = HuggingFaceHelper.NormalizeSourceId(sourceId);

            // Extract artifact name if present in the source ID
            string? artifactName = null;
            if (sourceId.Contains('/'))
            {
                var parts = sourceId.Split('/');
                if (parts.Length >= 3)
                {
                    artifactName = parts[parts.Length - 1];
                }
            }

            // First check if the model is already downloaded
            var existingModel = await _fileSystemRepository.GetModelAsync(sourceId, cancellationToken);
            if (existingModel != null && existingModel.IsLocal)
            {
                return ModelDownloadStatus.Completed;
            }

            // Check for model types to find the status file
            foreach (var modelType in Enum.GetValues<ModelType>())
            {
                // First try to find status file in the model directory
                var modelDir = _fileSystemRepository.GetModelDirectoryPath(sourceId, modelType);

                if (Directory.Exists(modelDir))
                {
                    // Try to find status file by artifact name
                    var statusFiles = Directory.GetFiles(modelDir, $"*{FileSystemHelper.DownloadStatusFileExtension}");

                    if (statusFiles.Length > 0)
                    {
                        // If we have a specific artifact name, look for that first
                        string? statusFilePath = null;

                        if (!string.IsNullOrEmpty(artifactName))
                        {
                            var sanitizedArtifactName = artifactName.Replace(':', '_').Replace('/', '_');
                            var specificStatusFile = Path.Combine(modelDir, $"{sanitizedArtifactName}{FileSystemHelper.DownloadStatusFileExtension}");

                            if (File.Exists(specificStatusFile))
                            {
                                statusFilePath = specificStatusFile;
                            }
                        }

                        // If we didn't find a specific file, use the first one
                        if (statusFilePath == null && statusFiles.Length > 0)
                        {
                            statusFilePath = statusFiles[0];
                        }

                        // Check the status file if we found one
                        if (statusFilePath != null && File.Exists(statusFilePath))
                        {
                            return await ParseDownloadStatusFileAsync(statusFilePath, modelDir, cancellationToken);
                        }
                    }
                }

                // Fallback to the old path
                var oldStatusFilePath = _fileSystemRepository.GetDownloadStatusFilePath(sourceId, modelType, artifactName);
                if (File.Exists(oldStatusFilePath))
                {
                    return await ParseDownloadStatusFileAsync(oldStatusFilePath, modelDir, cancellationToken);
                }
            }

            // Check the global downloads directory for status files matching the artifact name
            if (!string.IsNullOrEmpty(artifactName))
            {
                var downloadsDir = Path.Combine(_hubOptions.DataPath, FileSystemHelper.DownloadsDirectory);
                if (Directory.Exists(downloadsDir))
                {
                    var sanitizedArtifactName = artifactName.Replace(':', '_').Replace('/', '_');
                    var statusFile = Path.Combine(downloadsDir, $"{sanitizedArtifactName}{FileSystemHelper.DownloadStatusFileExtension}");

                    if (File.Exists(statusFile))
                    {
                        return await ParseDownloadStatusFileAsync(statusFile, null, cancellationToken);
                    }
                }
            }

            // No status file found
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting download status for model {ModelId}", sourceId);
            return null;
        }
    }

    /// <summary>
    /// Parses a download status file to determine the download status
    /// </summary>
    private async Task<ModelDownloadStatus> ParseDownloadStatusFileAsync(
        string statusFilePath,
        string? targetDir = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statusContent = await File.ReadAllTextAsync(statusFilePath, cancellationToken);

            // If it contains a "Paused" status, it's paused
            if (statusContent.Contains("\"Status\"") &&
                statusContent.Contains("\"Paused\""))
            {
                return ModelDownloadStatus.Paused;
            }

            // If it's a JSON structure with other status, check that
            if (statusContent.Contains("\"Status\"") &&
                statusContent.Contains("\"Failed\""))
            {
                return ModelDownloadStatus.Failed;
            }

            // If it's a JSON structure with no status or other status, assume it's downloading
            if (statusContent.StartsWith("{") && statusContent.EndsWith("}"))
            {
                return ModelDownloadStatus.Downloading;
            }

            // If it just contains a number (total size), it's an active download
            if (long.TryParse(statusContent.Trim(), out _))
            {
                // Check if there's been recent activity in the target directory
                if (!string.IsNullOrEmpty(targetDir) && Directory.Exists(targetDir))
                {
                    // If any file was modified in the last 30 seconds, consider it active
                    var recentFiles = Directory.GetFiles(targetDir, "*.*", SearchOption.AllDirectories)
                        .Where(f => (DateTime.UtcNow - new FileInfo(f).LastWriteTimeUtc).TotalSeconds < 30)
                        .Any();

                    return recentFiles ? ModelDownloadStatus.Downloading : ModelDownloadStatus.Paused;
                }

                return ModelDownloadStatus.Downloading;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing download status file: {FilePath}", statusFilePath);
        }

        // If we can't parse the status file, default to paused
        return ModelDownloadStatus.Paused;
    }
}