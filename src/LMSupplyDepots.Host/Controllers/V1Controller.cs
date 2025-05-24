using LMSupplyDepots.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Humanizer;
using LMSupplyDepots.Utils;

namespace LMSupplyDepots.Host.Controllers;

[ApiController]
[Route("/v1")]
public class V1Controller : ControllerBase
{
    private readonly IHostService _hostService;
    private readonly ILogger<V1Controller> _logger;

    /// <summary>
    /// Initializes a new instance of the InferenceController
    /// </summary>
    public V1Controller(IHostService hostService, ILogger<V1Controller> logger)
    {
        _hostService = hostService;
        _logger = logger;
    }

    /// <summary>
    /// Lists all loaded models with alias taking precedence over ID
    /// </summary>
    [HttpGet("models")]
    public async Task<ActionResult<ModelsListResponse>> ListModels(CancellationToken cancellationToken)
    {
        try
        {
            // Get all loaded models from the host service
            var loadedModels = await _hostService.GetLoadedModelsAsync(cancellationToken);

            // Convert to the response format, using Key (alias or id) for the name
            var response = new ModelsListResponse
            {
                Models = loadedModels.Select(m => new ModelListItem
                {
                    Type = m.Type.ToString().ToDashCase(),
                    Name = m.Key // This uses Alias if available, otherwise Id
                }).ToList()
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing models");
            return StatusCode(500, "An error occurred while listing models");
        }
    }

    /// <summary>
    /// Generates embeddings for the provided texts
    /// </summary>
    [HttpPost("embeddings")]
    public async Task<ActionResult<EmbeddingResponse>> GenerateEmbeddings(
        [FromBody] EmbeddingRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Model))
        {
            return BadRequest("Model is required");
        }

        if (request == null || request.Texts == null || request.Texts.Count == 0)
        {
            return BadRequest("Request must include at least one text to embed");
        }

        try
        {
            // Check if the model exists
            var model = await _hostService.GetModelAsync(request.Model, cancellationToken);
            if (model == null)
            {
                return NotFound($"Model '{request.Model}' not found");
            }

            // Check if the model supports embeddings
            if (!model.Capabilities.SupportsEmbeddings)
            {
                return BadRequest($"Model '{request.Model}' does not support embeddings");
            }

            // Generate embeddings
            var response = await _hostService.GenerateEmbeddingsAsync(request.Model, request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embeddings with model {Model}", request.Model);
            return StatusCode(500, $"Error generating embeddings: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates text based on the provided prompt
    /// </summary>
    [HttpPost("chat/completions")]
    public async Task<ActionResult<GenerationResponse>> GenerateText(
        [FromBody] GenerationRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.Model))
        {
            return BadRequest("Model is required");
        }

        if (request == null || string.IsNullOrEmpty(request.Prompt))
        {
            return BadRequest("Request must include a prompt");
        }

        try
        {
            // Check if the model exists
            var model = await _hostService.GetModelAsync(request.Model, cancellationToken);
            if (model == null)
            {
                return NotFound($"Model '{request.Model}' not found");
            }

            // Check if the model supports text generation
            if (model.Type != Models.ModelType.TextGeneration || !model.Capabilities.SupportsTextGeneration)
            {
                return BadRequest($"Model '{request.Model}' does not support text generation");
            }

            // Generate text
            var response = await _hostService.GenerateTextAsync(request.Model, request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating text with model {Model}", request.Model);
            return StatusCode(500, $"Error generating text: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates text with streaming response
    /// </summary>
    [HttpPost("chat/completions/stream")]
    public async Task GenerateTextStream(
        [FromBody] GenerationRequest request,
        [FromQuery] string modelId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(modelId))
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("Model ID is required");
            return;
        }

        if (request == null || string.IsNullOrEmpty(request.Prompt))
        {
            Response.StatusCode = 400;
            await Response.WriteAsync("Request must include a prompt");
            return;
        }

        try
        {
            // Check if the model exists
            var model = await _hostService.GetModelAsync(modelId, cancellationToken);
            if (model == null)
            {
                Response.StatusCode = 404;
                await Response.WriteAsync($"Model '{modelId}' not found");
                return;
            }

            // Check if the model supports text generation
            if (model.Type != Models.ModelType.TextGeneration || !model.Capabilities.SupportsTextGeneration)
            {
                Response.StatusCode = 400;
                await Response.WriteAsync($"Model '{modelId}' does not support text generation");
                return;
            }

            // Set the response type for streaming
            Response.ContentType = "text/event-stream";
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            // Force the request to be a streaming request
            request.Stream = true;

            // Stream the generated text
            await foreach (var token in _hostService.GenerateTextStreamAsync(modelId, request, cancellationToken))
            {
                var data = new { text = token };
                var json = JsonSerializer.Serialize(data);
                await Response.WriteAsync($"data: {json}\n\n");
                await Response.Body.FlushAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming text generation with model {ModelId}", modelId);
            Response.StatusCode = 500;
            await Response.WriteAsync($"Error generating text: {ex.Message}");
        }
    }
}