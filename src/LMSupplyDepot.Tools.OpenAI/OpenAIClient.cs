using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI Chat API
/// </summary>
public class OpenAIClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private const string ApiVersion = "v1";
    private const string BaseUrl = "https://api.openai.com";

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIClient"/> class
    /// </summary>
            public OpenAIClient(string apiKey, HttpClient? httpClient = null)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new ArgumentNullException(nameof(apiKey), "API key cannot be null or empty");
        }

        _httpClient = httpClient ?? new HttpClient();
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    #region Models

    /// <summary>
    /// Lists available models
    /// </summary>
    public async Task<ListModelsResponse> ListModelsAsync(CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<ListModelsResponse>(HttpMethod.Get, $"/{ApiVersion}/models", null, cancellationToken);
    }

    /// <summary>
    /// Retrieves a model
    /// </summary>
    public async Task<ModelInfo> RetrieveModelAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<ModelInfo>(HttpMethod.Get, $"/{ApiVersion}/models/{modelId}", null, cancellationToken);
    }

    #endregion

    #region Chat Completions

    /// <summary>
    /// Creates a chat completion
    /// </summary>
    public async Task<ChatCompletion> CreateChatCompletionAsync(CreateChatCompletionRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<ChatCompletion>(HttpMethod.Post, $"/{ApiVersion}/chat/completions", request, cancellationToken);
    }

    /// <summary>
    /// Creates a simple chat completion with a single user message
    /// </summary>
    public async Task<ChatCompletion> CreateSimpleChatCompletionAsync(string model, string userMessage, CancellationToken cancellationToken = default)
    {
        var request = CreateChatCompletionRequest.Create(model, new List<ChatMessage> { ChatMessage.FromUser(userMessage) });
        return await CreateChatCompletionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Creates a chat completion with a system message and a user message
    /// </summary>
    public async Task<ChatCompletion> CreateChatCompletionWithDeveloperMessageAsync(string model, string developerMessage, string userMessage, CancellationToken cancellationToken = default)
    {
        var request = CreateChatCompletionRequest.Create(
            model,
            new List<ChatMessage>
            {
                ChatMessage.FromDeveloper(developerMessage),
                ChatMessage.FromUser(userMessage)
            }
        );
        return await CreateChatCompletionAsync(request, cancellationToken);
    }

    /// <summary>
    /// Creates a streaming chat completion
    /// </summary>
    public async Task StreamChatCompletionAsync(CreateChatCompletionRequest request, Action<ChatCompletionChunk> onChunk, CancellationToken cancellationToken = default)
    {
        // Ensure streaming is enabled
        request.WithStream(true);

        var url = $"{BaseUrl}/{ApiVersion}/chat/completions";
        var requestJson = JsonSerializer.Serialize(request, _jsonOptions);

        // HTTP 요청 직접 구성
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        // HTTP 요청 전송 및 스트림 처리
        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        await EnsureSuccessStatusCodeAsync(response);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrEmpty(line))
                continue;

            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6).Trim();
                if (data == "[DONE]")
                    break;

                try
                {
                    var chunk = JsonSerializer.Deserialize<ChatCompletionChunk>(data, _jsonOptions);
                    if (chunk != null)
                    {
                        onChunk(chunk);
                    }
                }
                catch (JsonException ex)
                {
                    // 디버깅을 위해 오류 로깅 추가
                    Console.WriteLine($"JSON 파싱 오류: {ex.Message}, 데이터: {data}");
                    // Skip malformed JSON
                }
            }
        }
    }

    /// <summary>
    /// Gets only the text content from a chat completion response
    /// </summary>
    public string GetCompletionText(ChatCompletion response)
    {
        if (response?.Choices == null || response.Choices.Count == 0)
            return string.Empty;

        return response.Choices[0].Message.GetContentAsString();
    }

    /// <summary>
    /// Checks if a completion response has tool calls
    /// </summary>
    public bool HasToolCalls(ChatCompletion response)
    {
        if (response?.Choices == null || response.Choices.Count == 0)
            return false;

        return response.Choices[0].HasToolCalls();
    }

    /// <summary>
    /// Gets tool calls from a completion response
    /// </summary>
    public List<ToolCall> GetToolCalls(ChatCompletion response)
    {
        if (response?.Choices == null || response.Choices.Count == 0)
            return new List<ToolCall>();

        return response.Choices[0].GetToolCalls() ?? new List<ToolCall>();
    }

    /// <summary>
    /// Continues a conversation with tool outputs
    /// </summary>
    public async Task<ChatCompletion> ContinueWithToolOutputsAsync(
        string model,
        List<ChatMessage> messages,
        List<ToolOutput> toolOutputs,
        CancellationToken cancellationToken = default)
    {
        // Add the last assistant message
        var lastAssistantMessage = messages.LastOrDefault(m => m.Role == MessageRoles.Assistant);

        // Add a new user message with tool outputs
        var toolOutputsMessage = new ChatMessage { Role = MessageRoles.User };
        toolOutputsMessage.SetValue("tool_outputs", toolOutputs);

        var updatedMessages = new List<ChatMessage>(messages);
        updatedMessages.Add(toolOutputsMessage);

        // Create a new request
        var request = CreateChatCompletionRequest.Create(model, updatedMessages);

        // Send the request
        return await CreateChatCompletionAsync(request, cancellationToken);
    }

    #endregion

    #region Embeddings

    /// <summary>
    /// Creates embeddings
    /// </summary>
    public async Task<EmbeddingsResponse> CreateEmbeddingsAsync(CreateEmbeddingsRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<EmbeddingsResponse>(HttpMethod.Post, $"/{ApiVersion}/embeddings", request, cancellationToken);
    }

    /// <summary>
    /// Creates embeddings for a single text
    /// </summary>
    public async Task<EmbeddingsResponse> CreateEmbeddingAsync(string model, string text, int? dimensions = null, CancellationToken cancellationToken = default)
    {
        var request = CreateEmbeddingsRequest.Create(model, text);

        if (dimensions.HasValue)
        {
            request.WithDimensions(dimensions.Value);
        }

        return await CreateEmbeddingsAsync(request, cancellationToken);
    }

    /// <summary>
    /// Gets the embedding vector from a single-input embeddings response
    /// </summary>
    public List<float> GetEmbeddingVector(EmbeddingsResponse response)
    {
        if (response?.Data == null || response.Data.Count == 0)
            return new List<float>();

        return response.Data[0].Embedding;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Sends a request to the OpenAI API
    /// </summary>
    /// <typeparam name="T">The type of the response</typeparam>
    private async Task<T> SendRequestAsync<T>(HttpMethod method, string endpoint, object? requestBody = null, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}{endpoint}";
        var request = new HttpRequestMessage(method, url);

        if (requestBody != null)
        {
            var json = JsonSerializer.Serialize(requestBody, _jsonOptions);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessStatusCodeAsync(response);

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(responseContent, _jsonOptions);
    }

    /// <summary>
    /// Ensures that the response has a success status code, otherwise throws an exception with details
    /// </summary>
    private async Task EnsureSuccessStatusCodeAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var content = await response.Content.ReadAsStringAsync();
        var errorMessage = $"API error: {response.StatusCode}";

        try
        {
            var errorResponse = JsonSerializer.Deserialize<JsonElement>(content);
            if (errorResponse.TryGetProperty("error", out var error))
            {
                if (error.TryGetProperty("message", out var message))
                {
                    errorMessage = $"API error: {response.StatusCode} - {message}";
                }
            }
        }
        catch
        {
            // If we can't parse the error, just use the status code
        }

        throw new HttpRequestException(errorMessage, null, response.StatusCode);
    }

    #endregion
}