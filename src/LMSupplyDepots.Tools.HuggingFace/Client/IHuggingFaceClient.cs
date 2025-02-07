using LMSupplyDepots.Tools.HuggingFace.Download;
using LMSupplyDepots.Tools.HuggingFace.Models;

namespace LMSupplyDepots.Tools.HuggingFace.Client;

/// <summary>
/// Represents the core functionality for interacting with the Hugging Face API.
/// </summary>
public interface IHuggingFaceClient
{
    /// <summary>
    /// Asynchronously searches for models based on specified criteria.
    /// </summary>
    Task<IReadOnlyList<HuggingFaceModel>> SearchModelsAsync(
        string? search = null,
        string[]? filters = null,
        int limit = 5,
        string sort = "downloads",
        bool descending = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously finds a model by its repository ID.
    /// </summary>
    Task<HuggingFaceModel> FindModelByRepoIdAsync(
        string repoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously retrieves file information from a repository.
    /// </summary>
    Task<HuggingFaceFile> GetFileInfoAsync(
        string repoId,
        string filePath,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously downloads a file from a repository.
    /// </summary>
    IAsyncEnumerable<FileDownloadProgress> DownloadFileAsync(
        string repoId,
        string filePath,
        string outputPath,
        long startFrom = 0,
        CancellationToken cancellationToken = default);
}