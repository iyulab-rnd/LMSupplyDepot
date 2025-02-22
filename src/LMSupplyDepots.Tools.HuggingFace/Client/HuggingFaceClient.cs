using LMSupplyDepots.Tools.HuggingFace.Common;
using LMSupplyDepots.Tools.HuggingFace.Download;
using LMSupplyDepots.Tools.HuggingFace.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

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
                    using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var model = JsonSerializer.Deserialize<HuggingFaceModel>(json);

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

    /// <summary>
    /// Gets file information from a repository or directory.
    /// </summary>
    public async Task<IReadOnlyList<JsonElement>> GetRepositoryFilesAsync(
        string repoId,
        string? treePath = null,
        CancellationToken cancellationToken = default)
    {
        var path = treePath != null
            ? $"{repoId}/tree/main/{treePath}"
            : repoId;

        var requestUri = $"https://huggingface.co/api/models/{Uri.EscapeDataString(path)}";

        try
        {
            return await RetryHandler.ExecuteWithRetryAsync(
                async () =>
                {
                    using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var items = JsonSerializer.Deserialize<List<JsonElement>>(json);
                    return items ?? new List<JsonElement>();
                },
                _options.MaxRetries,
                _options.RetryDelayMilliseconds,
                _logger,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get repository files for '{Path}'", path);
            throw new HuggingFaceException(
                $"Failed to get repository files for '{path}'",
                (ex as HttpRequestException)?.StatusCode ?? HttpStatusCode.InternalServerError,
                ex);
        }
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

    /// <summary>
    /// Gets file sizes for all files in a repository including subdirectories.
    /// </summary>
    public async Task<Dictionary<string, long>> GetRepositoryFileSizesAsync(
        string repoId,
        CancellationToken cancellationToken = default)
    {
        var requestUri = $"https://huggingface.co/api/models/{Uri.EscapeDataString(repoId)}";

        try
        {
            var result = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
            var model = await FindModelByRepoIdAsync(repoId, cancellationToken);

            // siblings 배열에서 각 파일의 경로 추출
            var siblings = model.Siblings ?? [];
            // 하위경로에 GGUF 파일이 있는지 확인
            var subFiles = siblings.Where(p => p.Filename.Contains('/') && p.Filename.EndsWith(".gguf"));
            if (subFiles.Any())
            {
                var dirGroups = siblings
                    .Where(s => !string.IsNullOrEmpty(s.Filename))
                    .GroupBy(s => Path.GetDirectoryName(s.Filename))
                    .Where(g => !string.IsNullOrEmpty(g.Key));

                // 각 디렉토리별로 파일 크기 조회
                foreach (var group in dirGroups)
                {
                    var treePath = group.Key!.Replace('\\', '/');
                    await GetFileSizesInDirectoryAsync(repoId, treePath, result, cancellationToken);
                }
            }
            else
            {
                // 루트 디렉토리의 파일들 크기 조회
                var rootFiles = siblings
                        .Where(s => !string.IsNullOrEmpty(s.Filename) &&
                                   !s.Filename.Contains('/') &&
                                   !s.Filename.Contains('\\'));
                if (rootFiles.Any())
                {
                    await GetFileSizesInDirectoryAsync(repoId, "", result, cancellationToken);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to get repository file sizes for {RepoId}", repoId);
            throw new HuggingFaceException(
                $"Failed to get repository file sizes for '{repoId}'",
                (ex as HttpRequestException)?.StatusCode ?? HttpStatusCode.InternalServerError,
                ex);
        }
    }

    private async Task GetFileSizesInDirectoryAsync(
        string repoId,
        string treePath,
        Dictionary<string, long> results,
        CancellationToken cancellationToken)
    {
        var path = !string.IsNullOrEmpty(treePath)
            ? $"{repoId}/tree/main/{treePath}"
            : $"{repoId}/tree/main";

        // Uri.EscapeDataString 를 하면 안됩니다. (/를 그대로 사용되어야 함.) (주석삭제금지!)
        var requestUri = $"https://huggingface.co/api/models/{path}";

        try
        {
            var items = await RetryHandler.ExecuteWithRetryAsync(
                async () =>
                {
                    using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var treeItems = JsonSerializer.Deserialize<List<TreeItem>>(json);
                    return treeItems ?? [];
                },
                _options.MaxRetries,
                _options.RetryDelayMilliseconds,
                _logger,
                cancellationToken);

            foreach (var item in items)
            {
                if (item.Type == "file")
                {
                    var fullPath = item.Path;
                    results[fullPath] = item.Lfs?.Size ?? item.Size;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Failed to get repository tree for '{RepoId}' at path '{TreePath}'",
                repoId, treePath);
        }
    }

    private async Task GetRepositoryFileSizesInternalAsync(
        string repoId,
        string treePath,
        Dictionary<string, long> results,
        CancellationToken cancellationToken)
    {
        var path = !string.IsNullOrEmpty(treePath)
            ? $"{repoId}/tree/main/{treePath}"
            : repoId;

        var requestUri = $"https://huggingface.co/api/models/{Uri.EscapeDataString(path)}";

        try
        {
            var items = await RetryHandler.ExecuteWithRetryAsync(
                async () =>
                {
                    using var response = await _httpClient.GetAsync(requestUri, cancellationToken);
                    response.EnsureSuccessStatusCode();

                    var json = await response.Content.ReadAsStringAsync(cancellationToken);

                    // 응답을 JsonDocument로 먼저 파싱
                    using var document = JsonDocument.Parse(json);
                    var root = document.RootElement;

                    // 배열이 아닌 경우 빈 리스트 반환
                    if (root.ValueKind != JsonValueKind.Array)
                    {
                        return new List<TreeItem>();
                    }

                    var items = new List<TreeItem>();
                    foreach (var element in root.EnumerateArray())
                    {
                        var item = new TreeItem
                        {
                            Path = element.GetProperty("path").GetString() ?? "",
                            Type = element.GetProperty("type").GetString() ?? "",
                            Size = element.TryGetProperty("size", out var size) ? size.GetInt64() : 0
                        };

                        if (element.TryGetProperty("lfs", out var lfs))
                        {
                            item.Lfs = new TreeItem.LfsInfo
                            {
                                Size = lfs.GetProperty("size").GetInt64()
                            };
                        }

                        items.Add(item);
                    }

                    return items;
                },
                _options.MaxRetries,
                _options.RetryDelayMilliseconds,
                _logger,
                cancellationToken);

            foreach (var item in items)
            {
                if (item.Type == "tree") // directory type is "tree" in the API
                {
                    var subPath = string.IsNullOrEmpty(treePath)
                        ? item.Path
                        : $"{treePath}/{item.Path}";

                    await GetRepositoryFileSizesInternalAsync(
                        repoId, subPath, results, cancellationToken);
                }
                else if (item.Type == "blob") // file type is "blob" in the API
                {
                    var fullPath = string.IsNullOrEmpty(treePath)
                        ? item.Path
                        : $"{treePath}/{item.Path}";

                    results[fullPath] = item.Lfs?.Size ?? item.Size;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex,
                "Failed to get repository tree for '{RepoId}' at path '{TreePath}'",
                repoId, treePath);
            throw new HuggingFaceException(
                $"Failed to get repository tree for '{repoId}' at path '{treePath}'",
                (ex as HttpRequestException)?.StatusCode ?? HttpStatusCode.InternalServerError,
                ex);
        }
    }

    public async Task<FileDownloadResult> DownloadFileWithResultAsync(
        string repoId,
        string filePath,
        string outputPath,
        long startFrom = 0,
        IProgress<FileDownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(repoId);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);

        var requestUri = HuggingFaceConstants.UrlBuilder.CreateFileUrl(repoId, filePath);
        return await _downloadManager.DownloadWithResultAsync(
            requestUri,
            outputPath,
            startFrom,
            progress,
            cancellationToken);
    }

    /// <inheritdoc/>
    public string? GetDownloadUrl(string repoId, string filePath)
    {
        if (string.IsNullOrWhiteSpace(repoId) || string.IsNullOrWhiteSpace(filePath))
        {
            return null;
        }

        try
        {
            return HuggingFaceConstants.UrlBuilder.CreateFileUrl(repoId, filePath);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error creating download URL for {RepoId}/{FilePath}", repoId, filePath);
            return null;
        }
    }

    private class TreeItem
    {
        [JsonPropertyName("path")]
        public string Path { get; set; } = "";

        [JsonPropertyName("type")]
        public string Type { get; set; } = "";

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("lfs")]
        public LfsInfo? Lfs { get; set; }

        public class LfsInfo
        {
            [JsonPropertyName("size")]
            public long Size { get; set; }
        }
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