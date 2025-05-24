using Microsoft.AspNetCore.Mvc;
using LMSupplyDepots.ModelHub.Models;
using LMSupplyDepots.Models;
using Microsoft.Extensions.Logging;

namespace LMSupplyDepots.Host.Controllers;

/// <summary>
/// Controller for model download operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DownloadsController : ControllerBase
{
    private readonly IHostService _hostService;
    private readonly ILogger<DownloadsController> _logger;

    /// <summary>
    /// Initializes a new instance of the DownloadsController
    /// </summary>
    public DownloadsController(IHostService hostService, ILogger<DownloadsController> logger)
    {
        _hostService = hostService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all active downloads
    /// </summary>
    [HttpGet]
    public ActionResult<IEnumerable<KeyValuePair<string, ModelDownloadState>>> GetActiveDownloads()
    {
        try
        {
            var downloads = _hostService.GetActiveDownloads();
            return Ok(downloads);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active downloads");
            return StatusCode(500, "An error occurred while retrieving active downloads");
        }
    }

    /// <summary>
    /// Gets the status of a specific download
    /// </summary>
    [HttpGet("{modelId}/status")]
    public async Task<ActionResult<ModelDownloadStatus?>> GetDownloadStatus(string modelId)
    {
        try
        {
            var status = await _hostService.GetDownloadStatusAsync(modelId);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving download status for model {ModelId}", modelId);
            return StatusCode(500, $"An error occurred while retrieving download status for model {modelId}");
        }
    }

    /// <summary>
    /// Starts downloading a model
    /// </summary>
    [HttpPost("{modelId}")]
    public async Task<ActionResult<LMModel>> DownloadModel(string modelId)
    {
        try
        {
            // We can't use IProgress directly with API controllers, so we create a simple progress reporter
            var progressTracker = new ModelDownloadProgressTracker();

            // Start the download
            var model = await _hostService.DownloadModelAsync(modelId, progressTracker);
            return Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading model {ModelId}", modelId);
            return StatusCode(500, $"An error occurred while downloading model {modelId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Pauses a download
    /// </summary>
    [HttpPost("{modelId}/pause")]
    public async Task<ActionResult> PauseDownload(string modelId)
    {
        try
        {
            bool result = await _hostService.PauseDownloadAsync(modelId);
            if (!result)
            {
                return BadRequest($"Failed to pause download for model {modelId}");
            }
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing download for model {ModelId}", modelId);
            return StatusCode(500, $"An error occurred while pausing download for model {modelId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Resumes a paused download
    /// </summary>
    [HttpPost("{modelId}/resume")]
    public async Task<ActionResult<ModelDownloadState>> ResumeDownload(string modelId)
    {
        try
        {
            // Create a progress tracker
            var progressTracker = new ModelDownloadProgressTracker();

            // Resume the download
            var state = await _hostService.ResumeDownloadAsync(modelId, progressTracker);
            return Ok(state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming download for model {ModelId}", modelId);
            return StatusCode(500, $"An error occurred while resuming download for model {modelId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels a download
    /// </summary>
    [HttpPost("{modelId}/cancel")]
    public async Task<ActionResult> CancelDownload(string modelId)
    {
        try
        {
            bool result = await _hostService.CancelDownloadAsync(modelId);
            if (!result)
            {
                return BadRequest($"Failed to cancel download for model {modelId}");
            }
            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling download for model {ModelId}", modelId);
            return StatusCode(500, $"An error occurred while cancelling download for model {modelId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets information about a model without downloading it
    /// </summary>
    [HttpGet("{modelId}/info")]
    public async Task<ActionResult<LMModel>> GetModelInfo(string modelId)
    {
        try
        {
            var model = await _hostService.GetModelInfoAsync(modelId);
            return Ok(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting model info for {ModelId}", modelId);
            return StatusCode(500, $"An error occurred while getting model info for {modelId}: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets information about a repository
    /// </summary>
    [HttpGet("repository/{repoId}")]
    public async Task<ActionResult<LMRepo>> GetRepositoryInfo(string repoId)
    {
        try
        {
            var repo = await _hostService.GetRepositoryInfoAsync(repoId);
            return Ok(repo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repository info for {RepoId}", repoId);
            return StatusCode(500, $"An error occurred while getting repository info for {repoId}: {ex.Message}");
        }
    }
}

/// <summary>
/// Simple progress tracker for model downloads
/// </summary>
public class ModelDownloadProgressTracker : IProgress<ModelDownloadProgress>
{
    public ModelDownloadProgress? LastProgress { get; private set; }

    public void Report(ModelDownloadProgress value)
    {
        LastProgress = value;
    }
}