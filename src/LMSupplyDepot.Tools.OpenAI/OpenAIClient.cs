namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI Chat API
/// </summary>
public partial class OpenAIClient : OpenAIBaseClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIClient"/> class
    /// </summary>
    public OpenAIClient(string apiKey, HttpClient? httpClient = null)
        : base(apiKey, httpClient)
    {
    }

    /// <summary>
    /// Checks if the API key is valid by making a lightweight request to the API
    /// </summary>
    /// <returns>True if the API key is valid, false otherwise</returns>
    public async Task<bool> IsApiKeyValidAsync()
    {
        try
        {
            // Make a HEAD request to the models endpoint as a lightweight way to check API key validity
            var url = $"{BaseUrl}/{ApiVersion}/models";
            using var httpRequest = new HttpRequestMessage(HttpMethod.Head, url);

            // Send the request and check the response status code
            using var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead);

            // If we get a 200 OK response, the API key is valid
            // If we get a 401 Unauthorized, the API key is invalid
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
            // If any exception occurs during the request, consider the API key invalid
            return false;
        }
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

        // Configure HTTP request directly
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(requestJson, Encoding.UTF8, "application/json")
        };

        // Send HTTP request and process stream
        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        await EnsureSuccessStatusCodeAsync(response);

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
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
                    // Add logging for debugging
                    Debug.WriteLine($"JSON parsing error: {ex.Message}, Data: {data}");
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
}