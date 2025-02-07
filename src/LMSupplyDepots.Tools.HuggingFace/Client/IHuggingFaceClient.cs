using LMSupplyDepots.Tools.HuggingFace.Download;
using LMSupplyDepots.Tools.HuggingFace.Models;

namespace LMSupplyDepots.Tools.HuggingFace.Client;

/// <summary>
/// Represents the core functionality for interacting with the Hugging Face API.
/// </summary>
public interface IHuggingFaceClient : IDisposable
{
    /// <summary>
    /// Asynchronously searches for models based on specified criteria.
    /// </summary>
    /// <param name="search">Optional search term</param>
    /// <param name="filters">Optional array of filters</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="sortField">Field to sort results by</param>
    /// <param name="descending">Whether to sort in descending order</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<HuggingFaceModel>> SearchModelsAsync(
        string? search = null,
        string[]? filters = null,
        int limit = 5,
        ModelSortField sortField = ModelSortField.Downloads,
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

    /// <summary>
    /// Asynchronously downloads all files from a repository.
    /// </summary>
    /// <param name="repoId">The repository ID</param>
    /// <param name="outputDir">The directory where files will be saved</param>
    /// <param name="useSubDir">Whether to create a subdirectory for the repository</param>
    /// <param name="cancellationToken">Cancellation token</param>
    IAsyncEnumerable<RepoDownloadProgress> DownloadRepositoryAsync(
        string repoId,
        string outputDir,
        bool useSubDir = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously downloads specified files from a repository.
    /// </summary>
    /// <param name="repoId">The repository ID</param>
    /// <param name="filePaths">The specific file paths to download</param>
    /// <param name="outputDir">The directory where files will be saved</param>
    /// <param name="useSubDir">Whether to create a subdirectory for the repository</param>
    /// <param name="cancellationToken">Cancellation token</param>
    IAsyncEnumerable<RepoDownloadProgress> DownloadRepositoryFilesAsync(
        string repoId,
        IEnumerable<string> filePaths,
        string outputDir,
        bool useSubDir = true,
        CancellationToken cancellationToken = default);
}