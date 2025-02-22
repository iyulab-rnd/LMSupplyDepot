using LMSupplyDepots.Tools.HuggingFace.Download;
using LMSupplyDepots.Tools.HuggingFace.Models;
using System.Text.Json;

namespace LMSupplyDepots.Tools.HuggingFace.Client;

/// <summary>
/// Represents the core functionality for interacting with the Hugging Face API.
/// </summary>
public interface IHuggingFaceClient : IDisposable
{
    /// <summary>
    /// Asynchronously searches for text generation models.
    /// </summary>
    /// <param name="search">Optional search term</param>
    /// <param name="filters">Optional additional filters to apply</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="sortField">Field to sort results by</param>
    /// <param name="descending">Whether to sort in descending order</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<HuggingFaceModel>> SearchTextGenerationModelsAsync(
        string? search = null,
        string[]? filters = null,
        int limit = 5,
        ModelSortField sortField = ModelSortField.Downloads,
        bool descending = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously searches for embedding models.
    /// </summary>
    /// <param name="search">Optional search term</param>
    /// <param name="filters">Optional additional filters to apply</param>
    /// <param name="limit">Maximum number of results to return</param>
    /// <param name="sortField">Field to sort results by</param>
    /// <param name="descending">Whether to sort in descending order</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<IReadOnlyList<HuggingFaceModel>> SearchEmbeddingModelsAsync(
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

    Task<IReadOnlyList<JsonElement>> GetRepositoryFilesAsync(
        string repoId,
        string? treePath = null,
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

    Task<Dictionary<string, long>> GetRepositoryFileSizesAsync(
        string repoId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously downloads a file from a repository, using a single operation.
    /// </summary>
    Task<FileDownloadResult> DownloadFileWithResultAsync(
        string repoId,
        string filePath,
        string outputPath,
        long startFrom = 0,
        IProgress<FileDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the download URL for a specific file in a repository.
    /// </summary>
    /// <param name="repoId">The repository ID</param>
    /// <param name="filePath">The file path within the repository</param>
    /// <returns>The download URL for the file, or null if invalid parameters</returns>
    string? GetDownloadUrl(string repoId, string filePath);
}