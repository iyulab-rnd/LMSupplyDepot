namespace LMSupplyDepots.ModelHub.Models;

/// <summary>
/// Represents a model search result from an external source.
/// </summary>
public class ModelSearchResult
{
    /// <summary>
    /// Gets or sets the model information.
    /// </summary>
    public required LMModel Model { get; init; }

    /// <summary>
    /// Gets or sets the source of the model.
    /// </summary>
    public required string SourceName { get; init; }

    /// <summary>
    /// Gets or sets the source-specific ID of the model.
    /// </summary>
    public required string SourceId { get; init; }

    /// <summary>
    /// Gets or sets whether the model is already downloaded.
    /// </summary>
    public bool IsDownloaded { get; init; }

    /// <summary>
    /// Gets or sets whether the model is currently being downloaded.
    /// </summary>
    public bool IsDownloading { get; init; }

    /// <summary>
    /// Gets or sets whether the model download is paused.
    /// </summary>
    public bool IsPaused { get; init; }

    /// <summary>
    /// Gets whether the model is available for download.
    /// </summary>
    public bool IsAvailable => !IsDownloaded && !IsDownloading && !IsPaused;
}