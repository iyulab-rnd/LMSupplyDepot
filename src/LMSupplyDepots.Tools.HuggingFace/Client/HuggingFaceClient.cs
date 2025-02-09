using LMSupplyDepots.Tools.HuggingFace.Common;
using LMSupplyDepots.Tools.HuggingFace.Download;
using LMSupplyDepots.Tools.HuggingFace.Models;
using System.Net;
using System.Net.Http.Json;

namespace LMSupplyDepots.Tools.HuggingFace.Client;

/// <summary>
/// Client for interacting with the Hugging Face API.
/// </summary>
public class HuggingFaceClient : IHuggingFaceClient, IRepositoryDownloader, IDisposable
{
    private readonly HuggingFaceClientOptions _options;
    private readonly ILoggerFactory? _loggerFactory;
    private readonly ILogger<HuggingFaceClient>? _logger;
    private readonly HttpClient _httpClient;
    private readonly FileDownloadManager _downloadManager;
    private bool _disposed;

    public HuggingFaceClient(
        HuggingFaceClientOptions options,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        _options = options;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<HuggingFaceClient>();
        _httpClient = CreateHttpClient(_options);

        var downloadManagerLogger = loggerFactory?.CreateLogger<FileDownloadManager>();
        _downloadManager = new FileDownloadManager(_httpClient, downloadManagerLogger, _options.BufferSize);
    }

    public HuggingFaceClient(
        HuggingFaceClientOptions options,
        HttpMessageHandler handler,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();

        _options = options;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory?.CreateLogger<HuggingFaceClient>();
        _httpClient = new HttpClient(handler) { Timeout = options.Timeout };

        if (!string.IsNullOrWhiteSpace(options.Token))
        {
            _httpClient.DefaultRequestHeaders.Add(
                HuggingFaceConstants.Headers.Authorization,
                string.Format(HuggingFaceConstants.Headers.AuthorizationFormat, options.Token));
        }

        var downloadManagerLogger = loggerFactory?.CreateLogger<FileDownloadManager>();
        _downloadManager = new FileDownloadManager(_httpClient, downloadManagerLogger, _options.BufferSize);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<HuggingFaceModel>> SearchTextGenerationModelsAsync(
        string? search = null,
        string[]? filters = null,
        int limit = 5,
        ModelSortField sortField = ModelSortField.Downloads,
        bool descending = true,
        CancellationToken cancellationToken = default)
    {
        return SearchModelsInternalAsync(
            search,
            ModelFilters.TextGenerationFilters,
            filters,
            limit,
            sortField,
            descending,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<IReadOnlyList<HuggingFaceModel>> SearchEmbeddingModelsAsync(
        string? search = null,
        string[]? filters = null,
        int limit = 5,
        ModelSortField sortField = ModelSortField.Downloads,
        bool descending = true,
        CancellationToken cancellationToken = default)
    {
        return SearchModelsInternalAsync(
            search,
            ModelFilters.EmbeddingFilters,
            filters,
            limit,
            sortField,
            descending,
            cancellationToken);
    }

    private async Task<IReadOnlyList<HuggingFaceModel>> SearchModelsInternalAsync(
        string? search,
        IEnumerable<string> requiredFilters,
        string[]? additionalFilters,
        int limit,
        ModelSortField sortField,
        bool descending,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();

        try
        {
            var allFilters = (additionalFilters ?? [])
                .Concat(requiredFilters)
                .ToArray();

            var requestUri = HuggingFaceConstants.UrlBuilder.CreateModelSearchUrl(
                search,
                allFilters,
                limit,
                sortField.ToApiString(),
                descending);

            _logger?.LogInformation(
                "Searching models with URL: {RequestUri}\nParameters: search={Search}, filters={Filters}, limit={Limit}",
                requestUri, search, string.Join(", ", allFilters), limit);

            return await RetryHandler.ExecuteWithRetryAsync(
                async () =>
                {
                    var response = await _httpClient.GetAsync(requestUri, cancellationToken);
                    var content = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger?.LogError(
                            "API request failed: {StatusCode}\nResponse: {Content}",
                            response.StatusCode, content);

                        throw new HuggingFaceException(
                            $"API request failed with status code {response.StatusCode}",
                            response.StatusCode);
                    }

                    _logger?.LogDebug("API Response: {Content}", content);

                    var models = await response.Content.ReadFromJsonAsync<HuggingFaceModel[]>(
                        cancellationToken: cancellationToken) ?? Array.Empty<HuggingFaceModel>();

                    _logger?.LogInformation("Found {Count} models matching criteria", models.Length);

                    return models;
                },
                _options.MaxRetries,
                _options.RetryDelayMilliseconds,
                _logger,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error searching models");
            throw new HuggingFaceException(
                "Failed to search models",
                (ex as HttpRequestException)?.StatusCode ?? HttpStatusCode.InternalServerError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<HuggingFaceModel> FindModelByRepoIdAsync(
            string repoId,
            CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(repoId);

        var requestUri = HuggingFaceConstants.UrlBuilder.CreateModelUrl(repoId);

        try
        {
            _logger?.LogInformation("Finding model by repository ID: {RepoId}", repoId);

            return await RetryHandler.ExecuteWithRetryAsync(
                async () =>
                {
                    var model = await _httpClient.GetFromJsonAsync<HuggingFaceModel>(
                        requestUri, cancellationToken);
                    if (model == null)
                    {
                        throw new HuggingFaceException(
                            $"Model with repository ID '{repoId}' was not found.",
                            HttpStatusCode.NotFound);
                    }
                    return model;
                },
                _options.MaxRetries,
                _options.RetryDelayMilliseconds,
                _logger,
                cancellationToken);
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new HuggingFaceException(
                $"Model with repository ID '{repoId}' does not exist.",
                HttpStatusCode.NotFound, ex);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error finding model by repository ID: {RepoId}", repoId);
            throw new HuggingFaceException(
                $"Failed to find model with repository ID '{repoId}'",
                (ex as HttpRequestException)?.StatusCode ?? HttpStatusCode.InternalServerError,
                ex);
        }
    }

    /// <inheritdoc/>
    public async Task<HuggingFaceFile> GetFileInfoAsync(
        string repoId,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(repoId);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        var requestUri = HuggingFaceConstants.UrlBuilder.CreateFileUrl(repoId, filePath);

        try
        {
            _logger?.LogInformation("Getting file info: {RepoId}/{FilePath}", repoId, filePath);

            return await RetryHandler.ExecuteWithRetryAsync(
                async () =>
                {
                    var request = new HttpRequestMessage(HttpMethod.Head, requestUri);
                    using var response = await _httpClient.SendAsync(request,
                        HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                    response.EnsureSuccessStatusCode();

                    var fileInfo = new HuggingFaceFile
                    {
                        Name = Path.GetFileName(filePath),
                        Path = filePath,
                        Size = response.Content.Headers.ContentLength,
                        MimeType = response.Content.Headers.ContentType?.MediaType,
                        LastModified = response.Content.Headers.LastModified?.UtcDateTime
                    };

                    if (IsTextMimeType(fileInfo.MimeType))
                    {
                        using var getResponse = await _httpClient.GetAsync(requestUri, cancellationToken);
                        getResponse.EnsureSuccessStatusCode();
                        fileInfo.Content = await getResponse.Content.ReadAsStringAsync(cancellationToken);
                    }

                    return fileInfo;
                },
                _options.MaxRetries,
                _options.RetryDelayMilliseconds,
                _logger,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error getting file info: {RepoId}/{FilePath}", repoId, filePath);
            throw new HuggingFaceException($"Failed to get file info for '{repoId}/{filePath}'",
                (ex as HttpRequestException)?.StatusCode ?? HttpStatusCode.InternalServerError, ex);
        }
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<FileDownloadProgress> DownloadFileAsync(
        string repoId,
        string filePath,
        string outputPath,
        long startFrom = 0,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(repoId);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var requestUri = HuggingFaceConstants.UrlBuilder.CreateFileUrl(repoId, filePath);
        return _downloadManager.DownloadFileAsync(requestUri, outputPath, startFrom, cancellationToken);
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<RepoDownloadProgress> DownloadRepositoryAsync(
        string repoId,
        string outputDir,
        bool useSubDir = true,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var repoManagerLogger = _loggerFactory?.CreateLogger<RepositoryDownloadManager>();
        var downloader = new RepositoryDownloadManager(
            this,
            repoManagerLogger,
            _options.MaxConcurrentDownloads,
            _options.ProgressUpdateInterval);

        return downloader.DownloadRepositoryAsync(repoId, outputDir, useSubDir, cancellationToken);
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<RepoDownloadProgress> DownloadRepositoryFilesAsync(
        string repoId,
        IEnumerable<string> filePaths,
        string outputDir,
        bool useSubDir = true,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        var repoManagerLogger = _loggerFactory?.CreateLogger<RepositoryDownloadManager>();
        var downloader = new RepositoryDownloadManager(
            this,
            repoManagerLogger,
            _options.MaxConcurrentDownloads,
            _options.ProgressUpdateInterval);

        return downloader.DownloadRepositoryFilesAsync(repoId, filePaths, outputDir, useSubDir, cancellationToken);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }

    private static HttpClient CreateHttpClient(HuggingFaceClientOptions options)
    {
        var client = new HttpClient
        {
            Timeout = options.Timeout
        };

        if (!string.IsNullOrWhiteSpace(options.Token))
        {
            client.DefaultRequestHeaders.Add(
                HuggingFaceConstants.Headers.Authorization,
                string.Format(HuggingFaceConstants.Headers.AuthorizationFormat, options.Token));
        }

        return client;
    }

    private static bool IsTextMimeType(string? mimeType)
    {
        if (string.IsNullOrEmpty(mimeType))
            return false;

        return mimeType.StartsWith("text/", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Equals("application/json", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Equals("application/xml", StringComparison.OrdinalIgnoreCase) ||
               mimeType.Equals("application/javascript", StringComparison.OrdinalIgnoreCase);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HuggingFaceClient));
        }
    }
}