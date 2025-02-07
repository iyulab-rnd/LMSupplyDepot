using LMSupplyDepots.Tools.HuggingFace.Client;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace LMSupplyDepots.Tools.HuggingFace.Download;

/// <summary>
/// Manages repository download operations with concurrent file downloads and progress tracking.
/// </summary>
public sealed class RepositoryDownloadManager
{
    private readonly IHuggingFaceClient _client;
    private readonly ILogger<RepositoryDownloadManager>? _logger;
    private readonly int _maxConcurrentDownloads;
    private readonly int _progressUpdateInterval;

    /// <summary>
    /// Initializes a new instance of the RepositoryDownloadManager.
    /// </summary>
    public RepositoryDownloadManager(
        IHuggingFaceClient client,
        ILogger<RepositoryDownloadManager>? logger = null,
        int maxConcurrentDownloads = 5,
        int progressUpdateInterval = 100)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger;
        _maxConcurrentDownloads = maxConcurrentDownloads;
        _progressUpdateInterval = progressUpdateInterval;

        _logger?.LogInformation(
            "RepositoryDownloadManager initialized with maxConcurrentDownloads={MaxConcurrent}, updateInterval={Interval}ms",
            maxConcurrentDownloads, progressUpdateInterval);
    }

    /// <summary>
    /// Downloads a repository with concurrent file downloads and progress tracking.
    /// </summary>
    public async IAsyncEnumerable<RepoDownloadProgress> DownloadRepositoryAsync(
        string repoId,
        string outputDir,
        bool useSubDir = true,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(repoId);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDir);

        _logger?.LogInformation("Starting repository download: {RepoId} to {OutputDir}", repoId, outputDir);

        // Get repository information
        var model = await _client.FindModelByRepoIdAsync(repoId, cancellationToken);
        var files = model.GetFilePaths();

        if (!files.Any())
        {
            _logger?.LogWarning("No files found in repository: {RepoId}", repoId);
            yield break;
        }

        // Prepare output directory
        var targetDir = useSubDir ? Path.Combine(outputDir, repoId.Replace('/', '_')) : outputDir;
        Directory.CreateDirectory(targetDir);

        var progress = RepoDownloadProgress.Create(files);
        var downloadProgresses = new ConcurrentDictionary<string, FileDownloadProgress>();
        var completedFiles = new ConcurrentBag<string>();

        // Create download tasks for each file
        using var semaphore = new SemaphoreSlim(_maxConcurrentDownloads);
        var downloadTasks = new List<Task>();

        foreach (var file in files)
        {
            var task = ProcessFileDownloadAsync(
                repoId,
                file,
                targetDir,
                semaphore,
                downloadProgresses,
                completedFiles,
                cancellationToken);

            downloadTasks.Add(task);
        }

        // Monitor progress
        while (!downloadTasks.All(t => t.IsCompleted))
        {
            var currentProgress = progress with
            {
                CompletedFiles = completedFiles.ToImmutableHashSet(),
                CurrentProgresses = downloadProgresses.Values.ToImmutableList()
            };

            yield return currentProgress;
            await Task.Delay(_progressUpdateInterval, cancellationToken);
        }

        // Wait for all tasks to complete
        await Task.WhenAll(downloadTasks);

        _logger?.LogInformation("Repository download completed: {RepoId}", repoId);

        // Return final progress
        yield return progress.AsCompleted();
    }

    private async Task ProcessFileDownloadAsync(
        string repoId,
        string filePath,
        string targetDir,
        SemaphoreSlim semaphore,
        ConcurrentDictionary<string, FileDownloadProgress> progresses,
        ConcurrentBag<string> completedFiles,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);

        try
        {
            var outputPath = Path.Combine(targetDir, filePath);

            _logger?.LogInformation("Starting file download: {FilePath}", filePath);

            await foreach (var progress in _client.DownloadFileAsync(
                repoId, filePath, outputPath, cancellationToken: cancellationToken))
            {
                if (progress.IsCompleted)
                {
                    completedFiles.Add(filePath);
                    progresses.TryRemove(filePath, out _);
                    _logger?.LogInformation("File download completed: {FilePath}", filePath);
                }
                else
                {
                    progresses[filePath] = progress;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error downloading file: {FilePath}", filePath);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }
}