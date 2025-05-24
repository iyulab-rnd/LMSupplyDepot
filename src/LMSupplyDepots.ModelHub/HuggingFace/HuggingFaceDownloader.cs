using LMSupplyDepots.External.HuggingFace.Client;
using LMSupplyDepots.ModelHub.Repositories;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace LMSupplyDepots.ModelHub.HuggingFace;

/// <summary>
/// Implementation of IModelDownloader for Hugging Face models
/// </summary>
public partial class HuggingFaceDownloader : IModelDownloader, IDisposable
{
    private readonly HuggingFaceDownloaderOptions _options;
    private readonly ModelHubOptions _hubOptions;
    private readonly ILogger<HuggingFaceDownloader> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly FileSystemModelRepository _fileSystemRepository;
    private readonly Lazy<HuggingFaceClient> _client;
    private bool _disposed;

    private static readonly Regex _sourceIdRegex = new(@"^(hf|huggingface):(.+)$", RegexOptions.IgnoreCase);

    public string SourceName => "HuggingFace";

    /// <summary>
    /// Initializes a new instance of the HuggingFaceDownloader
    /// </summary>
    public HuggingFaceDownloader(
        IOptions<HuggingFaceDownloaderOptions> options,
        IOptions<ModelHubOptions> hubOptions,
        ILogger<HuggingFaceDownloader> logger,
        ILoggerFactory loggerFactory,
        FileSystemModelRepository fileSystemRepository)
    {
        _options = options.Value;
        _hubOptions = hubOptions.Value;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _fileSystemRepository = fileSystemRepository;

        _client = new Lazy<HuggingFaceClient>(() => CreateClient());
    }

    /// <summary>
    /// Creates a HuggingFaceClient with the configured options
    /// </summary>
    private HuggingFaceClient CreateClient()
    {
        var clientOptions = new HuggingFaceClientOptions
        {
            Token = _options.ApiToken,
            MaxConcurrentDownloads = _options.MaxConcurrentFileDownloads,
            Timeout = _options.RequestTimeout,
            MaxRetries = _options.MaxRetries
        };

        // Use the injected logger factory
        return new HuggingFaceClient(clientOptions, _loggerFactory);
    }

    /// <summary>
    /// Determines if this downloader can handle the given source ID
    /// </summary>
    public bool CanHandle(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return false;

        // Check if it matches typical HuggingFace patterns
        if (_sourceIdRegex.IsMatch(sourceId))
            return true;

        // If there's no prefix but it looks like a HuggingFace ID (contains a slash)
        // we also handle it as a default handler
        if (sourceId.Contains('/'))
            return true;

        // No explicit prefix and doesn't look like a HF id
        return false;
    }

    /// <summary>
    /// Gets available disk space for a path
    /// </summary>
    private static long GetAvailableDiskSpace(string path)
    {
        var driveInfo = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path)) ?? "C:\\");
        return driveInfo.AvailableFreeSpace;
    }

    /// <summary>
    /// Clean up resources
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing && _client.IsValueCreated)
            {
                _client.Value.Dispose();
            }

            _disposed = true;
        }
    }
}