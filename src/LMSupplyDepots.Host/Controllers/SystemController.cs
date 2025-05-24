﻿using Microsoft.AspNetCore.Mvc;
using LMSupplyDepots.Models;
using System.Reflection;
using LMSupplyDepots.ModelHub.Models;
using Microsoft.Extensions.Logging;

namespace LMSupplyDepots.Host.Controllers;

/// <summary>
/// Controller for general system operations
/// </summary>
[ApiController]
[Route("api")]
public class SystemController : ControllerBase
{
    private readonly IHostService _hostService;
    private readonly ILogger<SystemController> _logger;
    private readonly HostOptions _options;

    /// <summary>
    /// Initializes a new instance of the SystemController
    /// </summary>
    public SystemController(
        IHostService hostService,
        ILogger<SystemController> logger,
        Microsoft.Extensions.Options.IOptions<HostOptions> options)
    {
        _hostService = hostService;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Gets health information about the service
    /// </summary>
    [HttpGet("health")]
    public ActionResult<HealthInfo> GetHealth()
    {
        try
        {
            var healthInfo = new HealthInfo
            {
                Status = "OK",
                Version = GetVersion(),
                Timestamp = DateTime.UtcNow,
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
            };

            return Ok(healthInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting health information");
            return StatusCode(500, "An error occurred while getting health information");
        }
    }

    /// <summary>
    /// Searches for models from external sources
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ModelSearchResult>>> SearchModels(
        [FromQuery] string? type = null,
        [FromQuery] string? search = null,
        [FromQuery] int limit = 10)
    {
        try
        {
            ModelType? modelType = null;
            if (!string.IsNullOrEmpty(type) && Enum.TryParse<ModelType>(type, true, out var parsedType))
            {
                modelType = parsedType;
            }

            var results = await _hostService.SearchModelsAsync(modelType, search, limit);
            return Ok(results);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching for models");
            return StatusCode(500, "An error occurred while searching for models");
        }
    }

    /// <summary>
    /// Gets system configuration information
    /// </summary>
    [HttpGet("config")]
    public ActionResult<SystemConfig> GetConfig()
    {
        try
        {
            var config = new SystemConfig
            {
                ModelsDirectory = _options.DataPath,
                MaxConcurrentDownloads = _options.MaxConcurrentDownloads,
                MaxConcurrentOperations = _options.MaxConcurrentOperations,
                EnableModelCaching = _options.EnableModelCaching,
                MaxCachedModels = _options.MaxCachedModels,
                ForceCpuOnly = _options.ForceCpuOnly,
                LLamaThreads = _options.LLamaOptions?.Threads,
                LLamaGpuLayers = _options.LLamaOptions?.GpuLayers,
                LLamaContextSize = _options.LLamaOptions?.ContextSize
            };

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system configuration");
            return StatusCode(500, "An error occurred while getting system configuration");
        }
    }

    private string GetVersion()
    {
        return Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "1.0.0";
    }
}

/// <summary>
/// Health information
/// </summary>
public class HealthInfo
{
    public string Status { get; set; } = "";
    public string Version { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public string Environment { get; set; } = "";
}

/// <summary>
/// System configuration information
/// </summary>
public class SystemConfig
{
    public string ModelsDirectory { get; set; } = "";
    public int MaxConcurrentDownloads { get; set; }
    public int MaxConcurrentOperations { get; set; }
    public bool EnableModelCaching { get; set; }
    public int MaxCachedModels { get; set; }
    public bool ForceCpuOnly { get; set; }
    public int? LLamaThreads { get; set; }
    public int? LLamaGpuLayers { get; set; }
    public int? LLamaContextSize { get; set; }
}