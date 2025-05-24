namespace LMSupplyDepots.ModelHub.Models;

/// <summary>
/// Represents download status information for a model.
/// </summary>
public class ModelDownloadState
{
    /// <summary>
    /// Gets or sets the model ID.
    /// </summary>
    public required string ModelId { get; init; }

    /// <summary>
    /// Gets or sets the target directory where the model is being downloaded.
    /// </summary>
    public required string TargetDirectory { get; init; }

    /// <summary>
    /// Gets or sets the model type.
    /// </summary>
    public required ModelType ModelType { get; init; }

    /// <summary>
    /// Gets or sets the current status of the download.
    /// </summary>
    public ModelDownloadStatus Status { get; set; } = ModelDownloadStatus.Initializing;

    /// <summary>
    /// Gets or sets the timestamp when the download started.
    /// </summary>
    public DateTime StartTime { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the timestamp when the download was last updated.
    /// </summary>
    public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the total size of the download in bytes.
    /// This property is set-able (not init-only) so it can be updated during download.
    /// </summary>
    public long? TotalBytes { get; set; }

    /// <summary>
    /// Gets or sets the number of bytes downloaded so far.
    /// </summary>
    public long BytesDownloaded { get; set; }

    /// <summary>
    /// Gets or sets the cancellation token source for this download.
    /// </summary>
    public CancellationTokenSource? CancellationTokenSource { get; set; }

    /// <summary>
    /// Gets or sets the files that have been partially downloaded or completed.
    /// </summary>
    public Dictionary<string, long>? DownloadedFiles { get; set; }

    /// <summary>
    /// Gets or sets the provider-specific data that might be needed for resuming downloads.
    /// </summary>
    public Dictionary<string, string>? ProviderData { get; set; }

    /// <summary>
    /// Gets or sets any error message or information about the download.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Calculates the progress percentage of the download.
    /// </summary>
    public double ProgressPercentage =>
        TotalBytes.HasValue && TotalBytes > 0
            ? Math.Min(100.0, (BytesDownloaded * 100.0) / TotalBytes.Value)
            : 0;

    /// <summary>
    /// Gets the estimated time remaining based on the average download speed.
    /// </summary>
    public TimeSpan? EstimatedTimeRemaining
    {
        get
        {
            if (!TotalBytes.HasValue || TotalBytes.Value <= 0 || BytesDownloaded <= 0)
                return null;

            var elapsedTime = (LastUpdateTime - StartTime).TotalSeconds;
            if (elapsedTime <= 0)
                return null;

            var bytesPerSecond = BytesDownloaded / elapsedTime;
            if (bytesPerSecond <= 0)
                return null;

            var remainingBytes = TotalBytes.Value - BytesDownloaded;
            var secondsRemaining = remainingBytes / bytesPerSecond;

            return TimeSpan.FromSeconds(secondsRemaining);
        }
    }

    /// <summary>
    /// Gets the average download speed in bytes per second.
    /// </summary>
    public double AverageSpeed
    {
        get
        {
            var elapsedTime = (LastUpdateTime - StartTime).TotalSeconds;
            if (elapsedTime <= 0)
                return 0;

            return BytesDownloaded / elapsedTime;
        }
    }

    /// <summary>
    /// Creates a ModelDownloadProgress instance from this state.
    /// </summary>
    public ModelDownloadProgress ToProgress(string? fileName = null)
    {
        return new ModelDownloadProgress
        {
            ModelId = ModelId,
            FileName = fileName ?? Path.GetFileName(TargetDirectory),
            BytesDownloaded = BytesDownloaded,
            TotalBytes = TotalBytes,
            BytesPerSecond = AverageSpeed,
            EstimatedTimeRemaining = EstimatedTimeRemaining,
            Status = Status,
            ErrorMessage = Message
        };
    }
}