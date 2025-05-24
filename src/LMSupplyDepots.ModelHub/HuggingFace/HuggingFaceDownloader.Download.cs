using LMSupplyDepots.External.HuggingFace.Client;
using LMSupplyDepots.External.HuggingFace.Models;
using LMSupplyDepots.ModelHub.Utils;
using LMSupplyDepots.Utils;
using System.Net;
using System.Text.Json;

namespace LMSupplyDepots.ModelHub.HuggingFace;

/// <summary>
/// Implementation of model download operations
/// </summary>
public partial class HuggingFaceDownloader
{
    /// <summary>
    /// Downloads a model from Hugging Face to local storage
    /// </summary>
    public async Task<LMModel> DownloadModelAsync(
        string sourceId,
        string targetDirectory,
        IProgress<ModelDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting download of model {ModelId} to {TargetDir}", sourceId, targetDirectory);

        // Parse ID and extract actual HF repo ID and artifact name
        var (repoId, artifactName) = HuggingFaceHelper.NormalizeAndSplitSourceId(sourceId);

        // Ensure target directory exists
        Directory.CreateDirectory(targetDirectory);

        // Get model info for the repository
        HuggingFaceModel hfModel;
        try
        {
            hfModel = await _client.Value.FindModelByRepoIdAsync(repoId, cancellationToken);
        }
        catch (HuggingFaceException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            // Special handling for authentication errors
            var tokenProvided = !string.IsNullOrEmpty(_options.ApiToken);
            var message = tokenProvided
                ? $"The provided API token does not have sufficient permissions to access model '{repoId}'. This model may be private or gated."
                : $"Model '{repoId}' requires authentication. Please provide a HuggingFace API token.";

            _logger.LogError(ex, message);
            throw new ModelDownloadException(sourceId, message, ex);
        }

        // Convert to LMModel
        var model = HuggingFaceHelper.ConvertToLMModel(hfModel);

        // If an artifact name was specified, update the model with it
        if (!string.IsNullOrEmpty(artifactName))
        {
            model.ArtifactName = artifactName;
            model.Id = $"{model.Registry}:{model.RepoId}/{artifactName}";
        }

        model.LocalPath = targetDirectory;

        // Create download status file with artifact name
        var downloadFilePath = _fileSystemRepository.GetDownloadStatusFilePath(model.Id, model.Type, artifactName);

        // Calculate total size based on whether we're downloading a specific artifact or the whole repository
        long? totalSize;
        if (!string.IsNullOrEmpty(artifactName))
        {
            // Calculate size for the specific artifact
            totalSize = HuggingFaceHelper.CalculateArtifactSize(hfModel, artifactName);
            _logger.LogInformation("Estimated size for artifact {ArtifactName}: {Size} bytes",
                artifactName, totalSize?.ToString() ?? "unknown");
        }
        else
        {
            // Calculate size for the entire repository
            totalSize = HuggingFaceHelper.CalculateTotalSize(hfModel);
            _logger.LogInformation("Estimated total repository size: {Size} bytes",
                totalSize?.ToString() ?? "unknown");
        }

        if (totalSize.HasValue)
        {
            await File.WriteAllTextAsync(downloadFilePath, totalSize.Value.ToString(), cancellationToken);
        }

        try
        {
            // Check available disk space if we know the total size
            if (totalSize.HasValue && totalSize.Value > 0)
            {
                var availableSpace = GetAvailableDiskSpace(targetDirectory);
                if (availableSpace < totalSize.Value)
                {
                    throw new InsufficientDiskSpaceException(totalSize.Value, availableSpace);
                }
            }

            // Download progress handler
            var downloadProgress = new Progress<External.HuggingFace.Download.RepoDownloadProgress>(p =>
            {
                var currentFile = p.CurrentProgresses.FirstOrDefault();
                var status = p.IsCompleted
                    ? ModelDownloadStatus.Completed
                    : ModelDownloadStatus.Downloading;

                // Calculate total size based on the files in the repository
                long totalDownloadedBytes = 0;

                // Add size of completed files
                foreach (var completedFilePath in p.CompletedFiles)
                {
                    totalDownloadedBytes += HuggingFaceHelper.EstimateArtifactSize(
                        Path.GetFileNameWithoutExtension(completedFilePath),
                        Path.GetExtension(completedFilePath).TrimStart('.'));
                }

                // Add progress from current files being downloaded
                totalDownloadedBytes += p.CurrentProgresses.Sum(f => f.CurrentBytes);

                progress?.Report(new ModelDownloadProgress
                {
                    ModelId = sourceId,
                    FileName = currentFile?.UploadPath ?? "Unknown",
                    BytesDownloaded = totalDownloadedBytes,
                    TotalBytes = totalSize,
                    BytesPerSecond = currentFile?.DownloadSpeed ?? 0,
                    EstimatedTimeRemaining = currentFile?.RemainingTime,
                    Status = status
                });
            });

            // Determine specific files to download if an artifact name is specified
            if (!string.IsNullOrEmpty(artifactName))
            {
                _logger.LogInformation("Downloading specific artifact: {ArtifactName} from repository {RepoId}",
                    artifactName, repoId);

                // Find files that match the artifact name
                var filesToDownload = HuggingFaceHelper.FindArtifactFiles(hfModel, artifactName);

                if (filesToDownload.Count > 0)
                {
                    _logger.LogInformation("Found {Count} files for artifact {ArtifactName}: {Files}",
                        filesToDownload.Count, artifactName, string.Join(", ", filesToDownload));
                }
                else
                {
                    // If no matching files were found, try with the exact filename + extension
                    var exactFilename = $"{artifactName}.gguf"; // Default to .gguf extension
                    _logger.LogInformation("No matching files found, trying exact filename: {Filename}", exactFilename);
                    filesToDownload.Add(exactFilename);
                }

                // Download the specific files only
                var subdir = Path.Combine(targetDirectory, repoId.Replace('/', '_'));
                var actualTargetDir = targetDirectory;

                // Use the direct target directory instead of creating a subdirectory with the repoId
                await foreach (var _ in _client.Value.DownloadRepositoryFilesAsync(
                    repoId, filesToDownload, targetDirectory, false, cancellationToken))
                {
                    // Progress is reported through the progress handler
                }

                // If files were downloaded to a subdirectory, move them to the target directory
                if (Directory.Exists(subdir))
                {
                    _logger.LogInformation("Moving files from subdirectory {SubDir} to {TargetDir}", subdir, targetDirectory);

                    // Move all files from the subdirectory to the target directory
                    foreach (var file in Directory.GetFiles(subdir, "*.*", SearchOption.AllDirectories))
                    {
                        var fileName = Path.GetFileName(file);
                        var destinationPath = Path.Combine(targetDirectory, fileName);

                        // Create directories if needed
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

                        // Move the file
                        if (File.Exists(destinationPath))
                            File.Delete(destinationPath);

                        File.Move(file, destinationPath);
                    }

                    // Remove the empty subdirectory
                    try
                    {
                        Directory.Delete(subdir, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete subdirectory {SubDir}", subdir);
                    }
                }
            }
            else
            {
                // Download the entire repository but without creating a subdirectory with the repoId
                _logger.LogInformation("Downloading entire repository: {RepoId}", repoId);
                await foreach (var _ in _client.Value.DownloadRepositoryAsync(
                    repoId, targetDirectory, false, cancellationToken))
                {
                    // Progress is reported through the progress handler
                }

                // If files were downloaded to a subdirectory, move them to the target directory
                var subdir = Path.Combine(targetDirectory, repoId.Replace('/', '_'));
                if (Directory.Exists(subdir))
                {
                    _logger.LogInformation("Moving files from subdirectory {SubDir} to {TargetDir}", subdir, targetDirectory);

                    // Move all files from the subdirectory to the target directory
                    foreach (var file in Directory.GetFiles(subdir, "*.*", SearchOption.AllDirectories))
                    {
                        var fileName = Path.GetFileName(file);
                        var destinationPath = Path.Combine(targetDirectory, fileName);

                        // Create directories if needed
                        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

                        // Move the file
                        if (File.Exists(destinationPath))
                            File.Delete(destinationPath);

                        File.Move(file, destinationPath);
                    }

                    // Remove the empty subdirectory
                    try
                    {
                        Directory.Delete(subdir, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not delete subdirectory {SubDir}", subdir);
                    }
                }
            }

            // Move metadata alongside the model files
            string? mainModelFile = null;
            if (model.Type == ModelType.TextGeneration || model.Type == ModelType.Embedding)
            {
                // Find the downloaded model file
                // If a specific artifact was requested, look for that file specifically
                if (!string.IsNullOrEmpty(artifactName))
                {
                    // First, try to find the exact artifact name
                    var searchPattern = $"{artifactName}.*";
                    mainModelFile = Directory.GetFiles(targetDirectory, searchPattern, SearchOption.AllDirectories)
                        .FirstOrDefault(f => Path.GetExtension(f).TrimStart('.').ToLowerInvariant() is "gguf" or "bin" or "safetensors");

                    if (mainModelFile == null)
                    {
                        // Try with case-insensitive search
                        mainModelFile = Directory.GetFiles(targetDirectory, "*.*", SearchOption.AllDirectories)
                            .FirstOrDefault(f =>
                                (Path.GetExtension(f).TrimStart('.').ToLowerInvariant() is "gguf" or "bin" or "safetensors") &&
                                Path.GetFileNameWithoutExtension(f).Equals(artifactName, StringComparison.OrdinalIgnoreCase));
                    }

                    // If still not found, try files containing the name
                    if (mainModelFile == null)
                    {
                        mainModelFile = Directory.GetFiles(targetDirectory, "*.*", SearchOption.AllDirectories)
                            .Where(f =>
                                (Path.GetExtension(f).TrimStart('.').ToLowerInvariant() is "gguf" or "bin" or "safetensors") &&
                                Path.GetFileNameWithoutExtension(f).Contains(artifactName, StringComparison.OrdinalIgnoreCase))
                            .OrderByDescending(f => new FileInfo(f).Length)
                            .FirstOrDefault();
                    }
                }

                // If no specific file found, get the largest model file
                if (mainModelFile == null)
                {
                    mainModelFile = Directory.GetFiles(targetDirectory, "*.gguf", SearchOption.AllDirectories)
                        .Concat(Directory.GetFiles(targetDirectory, "*.bin", SearchOption.AllDirectories))
                        .Concat(Directory.GetFiles(targetDirectory, "*.safetensors", SearchOption.AllDirectories))
                        .OrderByDescending(f => new FileInfo(f).Length)
                        .FirstOrDefault();
                }
            }

            // If we found a main model file, use its name as the base for metadata
            if (mainModelFile != null)
            {
                // Update model properties based on the actual file
                model.Format = Path.GetExtension(mainModelFile).TrimStart('.');
                if (string.IsNullOrEmpty(model.ArtifactName) || !string.IsNullOrEmpty(artifactName))
                {
                    model.ArtifactName = Path.GetFileNameWithoutExtension(mainModelFile);
                }
                model.FilePaths = new List<string> { Path.GetFileName(mainModelFile) };
                model.SizeInBytes = new FileInfo(mainModelFile).Length;

                var modelBaseName = Path.GetFileNameWithoutExtension(mainModelFile);
                var metadataPath = Path.Combine(Path.GetDirectoryName(mainModelFile) ?? targetDirectory, $"{modelBaseName}.json");

                // Save metadata alongside the model
                var json = JsonHelper.Serialize(model);

                await File.WriteAllTextAsync(metadataPath, json, cancellationToken);
                _logger.LogInformation("Created metadata file: {MetadataPath}", metadataPath);
            }

            _logger.LogInformation("Completed download of model {ModelId}", sourceId);

            // Remove download status file after successful download
            if (File.Exists(downloadFilePath))
            {
                try
                {
                    File.Delete(downloadFilePath);
                    _logger.LogDebug("Removed download status file: {FilePath}", downloadFilePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete download status file: {FilePath}", downloadFilePath);
                }
            }

            // Also check for and remove any status files in the model directory
            if (mainModelFile != null)
            {
                var modelDir = Path.GetDirectoryName(mainModelFile);
                if (!string.IsNullOrEmpty(modelDir) && Directory.Exists(modelDir))
                {
                    try
                    {
                        var statusFiles = Directory.GetFiles(modelDir, $"*{FileSystemHelper.DownloadStatusFileExtension}");
                        foreach (var statusFile in statusFiles)
                        {
                            File.Delete(statusFile);
                            _logger.LogDebug("Removed local status file: {FilePath}", statusFile);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to clean up status files in model directory: {Dir}", modelDir);
                    }
                }
            }

            return model;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading model {ModelId}: {Message}", sourceId, ex.Message);

            // Update the status file with error information if possible
            try
            {
                if (File.Exists(downloadFilePath))
                {
                    var statusInfo = new
                    {
                        ModelId = sourceId,
                        ArtifactName = artifactName,
                        Error = ex.Message,
                        ErrorTime = DateTime.UtcNow,
                        Status = "Failed"
                    };

                    var json = JsonHelper.Serialize(statusInfo);
                    await File.WriteAllTextAsync(downloadFilePath, json, cancellationToken);
                    _logger.LogDebug("Updated download status file with error information: {FilePath}", downloadFilePath);
                }
            }
            catch (Exception statusEx)
            {
                // Ignore errors when updating the status file
                _logger.LogWarning(statusEx, "Failed to update download status file with error: {FilePath}", downloadFilePath);
            }

            // Special handling for known error types
            if (ex is HuggingFaceException hfEx && hfEx.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new ModelDownloadException(sourceId,
                    "This model requires authentication. Please provide a valid HuggingFace API token.", ex);
            }

            throw new ModelDownloadException(sourceId, $"Download failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Resumes a previously paused download
    /// </summary>
    public async Task<LMModel> ResumeDownloadAsync(
        string sourceId,
        IProgress<ModelDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Attempting to resume download for model {ModelId}", sourceId);

        // Check if the download is paused
        var downloadStatus = await GetDownloadStatusAsync(sourceId, cancellationToken);
        if (downloadStatus != ModelDownloadStatus.Paused)
        {
            _logger.LogWarning("Cannot resume download for model {ModelId} - not in paused state", sourceId);
            throw new InvalidOperationException($"Download for model {sourceId} is not paused");
        }

        try
        {
            // Get the model type
            var normalizedId = HuggingFaceHelper.NormalizeSourceId(sourceId);
            var modelType = await HuggingFaceHelper.DetermineModelTypeAsync(normalizedId, _client.Value, cancellationToken);

            // Get the target directory from the saved status file
            var statusFilePath = _fileSystemRepository.GetDownloadStatusFilePath(sourceId, modelType);
            string targetDirectory;

            // Load download state from the status file
            if (File.Exists(statusFilePath))
            {
                var statusContent = await File.ReadAllTextAsync(statusFilePath, cancellationToken);

                try
                {
                    var statusInfo = JsonHelper.Deserialize<JsonElement>(statusContent);

                    if (statusInfo.TryGetProperty("TargetDirectory", out var targetDirElement))
                    {
                        targetDirectory = targetDirElement.GetString() ??
                            _fileSystemRepository.GetModelDirectoryPath(sourceId, modelType);
                    }
                    else
                    {
                        targetDirectory = _fileSystemRepository.GetModelDirectoryPath(sourceId, modelType);
                    }

                    // Update the status file to indicate it's downloading again
                    if (statusInfo.TryGetProperty("TotalSize", out var totalSizeElement) &&
                        totalSizeElement.TryGetInt64(out var totalSize))
                    {
                        // Convert back to a simple size-only status file
                        await File.WriteAllTextAsync(statusFilePath, totalSize.ToString(), cancellationToken);
                    }
                }
                catch
                {
                    // If we can't parse the JSON, just use the default path
                    targetDirectory = _fileSystemRepository.GetModelDirectoryPath(sourceId, modelType);
                }
            }
            else
            {
                targetDirectory = _fileSystemRepository.GetModelDirectoryPath(sourceId, modelType);
            }

            // Ensure the target directory exists
            Directory.CreateDirectory(targetDirectory);

            // Resume the download by calling the normal download method
            // The HuggingFace client will handle resuming based on existing files
            return await DownloadModelAsync(sourceId, targetDirectory, progress, cancellationToken);
        }
        catch (Exception ex) when (!(ex is InvalidOperationException))
        {
            _logger.LogError(ex, "Error resuming download for model {ModelId}", sourceId);
            throw new InvalidOperationException($"Failed to resume download for model {sourceId}", ex);
        }
    }
}