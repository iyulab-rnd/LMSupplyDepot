using System.Collections.Generic;
using System.Threading;

namespace LMSupplyDepots.Tools.HuggingFace.Download;

/// <summary>
/// Defines methods for downloading Hugging Face repositories.
/// </summary>
public interface IRepositoryDownloader
{
    /// <summary>
    /// Downloads a complete repository with progress tracking.
    /// </summary>
    /// <param name="repoId">The repository ID to download.</param>
    /// <param name="outputDir">The directory where files will be saved.</param>
    /// <param name="useSubDir">Whether to create a subdirectory for the repository.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>An async enumerable of download progress updates.</returns>
    IAsyncEnumerable<RepoDownloadProgress> DownloadRepositoryAsync(
        string repoId,
        string outputDir,
        bool useSubDir = true,
        CancellationToken cancellationToken = default);
}