using LMSupplyDepots.Tools.HuggingFace.Client;
using System.Net;
using System.Runtime.CompilerServices;

namespace LMSupplyDepots.Tools.HuggingFace.Download;

/// <summary>
/// Manages file download operations with progress tracking.
/// </summary>
internal sealed class FileDownloadManager
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FileDownloadManager>? _logger;
    private readonly int _defaultBufferSize;

    public FileDownloadManager(
        HttpClient httpClient,
        ILogger<FileDownloadManager>? logger = null,
        int defaultBufferSize = 8192)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger;
        _defaultBufferSize = defaultBufferSize;
    }

    /// <summary>
    /// Downloads a file from the specified URL with progress tracking.
    /// </summary>
    public async IAsyncEnumerable<FileDownloadProgress> DownloadFileAsync(
        string url,
        string outputPath,
        long startFrom = 0,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Starting download from {Url} to {OutputPath}", url, outputPath);

        var request = CreateRequest(url, startFrom);
        using var response = await SendRequestAsync(request, cancellationToken);
        var totalBytes = GetTotalBytes(response, startFrom);

        _logger?.LogInformation("Total file size: {TotalBytes} bytes", totalBytes);

        EnsureDirectory(outputPath);

        var bufferSize = DetermineOptimalBufferSize(totalBytes);
        await using var fileStream = CreateFileStream(outputPath, startFrom, bufferSize);
        using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);

        var progressTracker = new DownloadProgressTracker(startFrom, DateTime.UtcNow);
        var buffer = new byte[bufferSize];

        while (!cancellationToken.IsCancellationRequested)
        {
            var bytesRead = await contentStream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0) break;

            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);

            var progress = progressTracker.UpdateProgress(bytesRead);
            var remainingTime = CalculateRemainingTime(progress.TotalBytesRead, totalBytes, progress.DownloadSpeed);

            yield return FileDownloadProgress.CreateProgress(
                outputPath,
                progress.TotalBytesRead,
                totalBytes,
                progress.DownloadSpeed,
                remainingTime);
        }

        _logger?.LogInformation("Download completed: {OutputPath}", outputPath);
        yield return FileDownloadProgress.CreateCompleted(outputPath, progressTracker.TotalBytesRead);
    }

    private HttpRequestMessage CreateRequest(string url, long startFrom)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        if (startFrom > 0)
        {
            request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(startFrom, null);
            _logger?.LogInformation("Resuming download from byte position {StartFrom}", startFrom);
        }
        return request;
    }

    private async Task<HttpResponseMessage> SendRequestAsync(
    HttpRequestMessage request,
    CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new HuggingFaceException(
                    "This model requires authentication. Please provide a valid API token with the necessary permissions.",
                    HttpStatusCode.Unauthorized);
            }

            response.EnsureSuccessStatusCode();
            return response;
        }
        catch (HttpRequestException ex)
        {
            _logger?.LogError(ex, "HTTP request failed: {Url}", request.RequestUri);

            var message = ex.StatusCode == HttpStatusCode.Unauthorized
                ? "This model requires authentication. Please provide a valid API token with the necessary permissions."
                : $"Failed to download file: {request.RequestUri}";

            throw new HuggingFaceException(message, ex.StatusCode ?? HttpStatusCode.InternalServerError, ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Request failed: {Url}", request.RequestUri);
            throw new HuggingFaceException(
                $"Failed to download file: {request.RequestUri}",
                HttpStatusCode.InternalServerError,
                ex);
        }
    }

    private static long? GetTotalBytes(HttpResponseMessage response, long startFrom)
    {
        return response.Content.Headers.ContentLength.HasValue
            ? response.Content.Headers.ContentLength.Value + startFrom
            : null;
    }

    private static void EnsureDirectory(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (string.IsNullOrEmpty(directory))
        {
            throw new InvalidOperationException($"Invalid directory path: {path}");
        }

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private int DetermineOptimalBufferSize(long? totalBytes)
    {
        // If file size is unknown or very small, use default buffer size
        if (!totalBytes.HasValue || totalBytes.Value < _defaultBufferSize)
            return _defaultBufferSize;

        // For larger files, scale buffer size with file size, but cap it
        var optimalSize = (int)Math.Min(
            Math.Max(
                _defaultBufferSize,
                Math.Min(totalBytes.Value / 100, 1024 * 1024) // 1MB max
            ),
            Environment.SystemPageSize * 16 // But also consider system page size
        );

        _logger?.LogDebug("Determined optimal buffer size: {BufferSize} bytes", optimalSize);
        return optimalSize;
    }

    private static FileStream CreateFileStream(string path, long startFrom, int bufferSize)
    {
        return new FileStream(
            path,
            startFrom > 0 ? FileMode.Append : FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
    }

    private static TimeSpan? CalculateRemainingTime(long bytesDownloaded, long? totalBytes, double downloadSpeed)
    {
        if (!totalBytes.HasValue || downloadSpeed <= 0)
            return null;

        var remainingBytes = totalBytes.Value - bytesDownloaded;
        return TimeSpan.FromSeconds(remainingBytes / downloadSpeed);
    }

    private class DownloadProgressTracker
    {
        private readonly DateTime _startTime;
        private readonly List<(DateTime timestamp, long bytes)> _recentChunks;
        private const int MaxRecentChunks = 10; // Track last 10 chunks for speed calculation

        public long TotalBytesRead { get; private set; }

        public DownloadProgressTracker(long initialBytes, DateTime startTime)
        {
            TotalBytesRead = initialBytes;
            _startTime = startTime;
            _recentChunks = [];
        }

        public (long TotalBytesRead, double DownloadSpeed) UpdateProgress(int newBytes)
        {
            TotalBytesRead += newBytes;
            var now = DateTime.UtcNow;

            _recentChunks.Add((now, newBytes));
            if (_recentChunks.Count > MaxRecentChunks)
                _recentChunks.RemoveAt(0);

            var recentTimeSpan = (now - _recentChunks[0].timestamp).TotalSeconds;
            var recentBytes = _recentChunks.Sum(chunk => chunk.bytes);
            var recentSpeed = recentTimeSpan > 0 ? recentBytes / recentTimeSpan : 0;

            var overallTimeSpan = (now - _startTime).TotalSeconds;
            var overallSpeed = overallTimeSpan > 0 ? TotalBytesRead / overallTimeSpan : 0;

            // Use recent speed if available, fall back to overall speed if recent speed is too low
            var speed = recentSpeed > overallSpeed / 2 ? recentSpeed : overallSpeed;

            return (TotalBytesRead, speed);
        }
    }
}