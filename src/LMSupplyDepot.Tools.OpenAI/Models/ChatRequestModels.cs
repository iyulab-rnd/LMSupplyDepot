namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Constants for finish reasons
/// </summary>
public static class FinishReasons
{
    public const string Stop = "stop";
    public const string Length = "length";
    public const string ToolCalls = "tool_calls";
    public const string ContentFilter = "content_filter";
    public const string FunctionCall = "function_call";
}

/// <summary>
/// Request model for creating a chat completion
/// </summary>
public class CreateChatCompletionRequest : BaseRequest
{
    /// <summary>
    /// ID of the model to use
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// The messages to generate chat completions for
    /// </summary>
    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; }

    /// <summary>
    /// Whether to store the completion for future reference (default: false)
    /// </summary>
    [JsonPropertyName("store")]
    public bool? Store { get; set; }

    /// <summary>
    /// Creates a new CreateChatCompletionRequest with the specified model and messages
    /// </summary>
    public static CreateChatCompletionRequest Create(string model, List<ChatMessage> messages)
    {
        return new CreateChatCompletionRequest
        {
            Model = model,
            Messages = messages
        };
    }

    /// <summary>
    /// Sets the temperature for the request
    /// </summary>
    public CreateChatCompletionRequest WithTemperature(double temperature)
    {
        SetValue("temperature", temperature);
        return this;
    }

    /// <summary>
    /// Sets the top_p for the request
    /// </summary>
    public CreateChatCompletionRequest WithTopP(double topP)
    {
        SetValue("top_p", topP);
        return this;
    }

    /// <summary>
    /// Sets the number of chat completion choices to generate
    /// </summary>
    public CreateChatCompletionRequest WithN(int n)
    {
        SetValue("n", n);
        return this;
    }

    /// <summary>
    /// Sets whether to stream back partial progress
    /// </summary>
    public CreateChatCompletionRequest WithStream(bool stream)
    {
        SetValue("stream", stream);
        return this;
    }

    /// <summary>
    /// Sets the maximum number of tokens to generate
    /// </summary>
    public CreateChatCompletionRequest WithMaxTokens(int maxTokens)
    {
        SetValue("max_tokens", maxTokens);
        return this;
    }

    /// <summary>
    /// Sets the presence penalty for the request
    /// </summary>
    public CreateChatCompletionRequest WithPresencePenalty(double presencePenalty)
    {
        SetValue("presence_penalty", presencePenalty);
        return this;
    }

    /// <summary>
    /// Sets the frequency penalty for the request
    /// </summary>
    public CreateChatCompletionRequest WithFrequencyPenalty(double frequencyPenalty)
    {
        SetValue("frequency_penalty", frequencyPenalty);
        return this;
    }

    /// <summary>
    /// Sets the logit bias for the request
    /// </summary>
    public CreateChatCompletionRequest WithLogitBias(Dictionary<string, int> logitBias)
    {
        SetValue("logit_bias", logitBias);
        return this;
    }

    /// <summary>
    /// Sets the stop sequences for the request
    /// </summary>
    public CreateChatCompletionRequest WithStop(List<string> stop)
    {
        SetValue("stop", stop);
        return this;
    }

    /// <summary>
    /// Sets the stop sequence for the request
    /// </summary>
    public CreateChatCompletionRequest WithStop(string stop)
    {
        SetValue("stop", new List<string> { stop });
        return this;
    }

    /// <summary>
    /// Sets a user identifier for the request
    /// </summary>
    public CreateChatCompletionRequest WithUser(string user)
    {
        SetValue("user", user);
        return this;
    }

    /// <summary>
    /// Sets whether to enable reasoning mode for the model
    /// </summary>
    public CreateChatCompletionRequest WithReasoning(bool reasoning)
    {
        SetValue("reasoning", reasoning);
        return this;
    }

    /// <summary>
    /// Sets the tools for the request
    /// </summary>
    public CreateChatCompletionRequest WithTools(List<Tool> tools)
    {
        SetValue("tools", tools);
        return this;
    }

    /// <summary>
    /// Adds a function tool to the request
    /// </summary>
    public CreateChatCompletionRequest WithFunctionTool(string name, string description, object parameters)
    {
        var tools = GetValue<List<Tool>>("tools") ?? new List<Tool>();
        tools.Add(Tool.CreateFunctionTool(name, description, parameters));
        SetValue("tools", tools);
        return this;
    }

    /// <summary>
    /// Sets the response format for structured outputs
    /// </summary>
    public CreateChatCompletionRequest WithResponseFormat(object responseFormat)
    {
        SetValue("response_format", responseFormat);
        return this;
    }

    /// <summary>
    /// Sets to store the chat completion
    /// </summary>
    public CreateChatCompletionRequest WithStore(bool store)
    {
        Store = store;
        return this;
    }
}

/// <summary>
/// Response format for structured JSON outputs
/// </summary>
public class JsonResponseFormat : BaseModel
{
    /// <summary>
    /// Creates a new JSON response format
    /// </summary>
    public static JsonResponseFormat Create(object? schema = null)
    {
        var format = new JsonResponseFormat();
        format.SetValue("type", "json_schema");

        if (schema != null)
        {
            format.SetValue("json_schema", schema);
        }

        return format;
    }
}

/// <summary>
/// Represents a chat delta in a streaming response
/// </summary>
public class ChatDelta : BaseModel
{
    /// <summary>
    /// The role of the author
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; }

    /// <summary>
    /// The content of the delta
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; }

    /// <summary>
    /// Gets the content of the delta
    /// </summary>
    public string GetContent()
    {
        // 직접 Content 속성 반환
        return Content ?? string.Empty;
    }
}

/// <summary>
/// Represents a choice in a streaming chat completion response
/// </summary>
public class ChatCompletionChunkChoice : BaseModel
{
    /// <summary>
    /// The index of the choice
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// The delta for this chunk
    /// </summary>
    [JsonPropertyName("delta")]
    public ChatDelta Delta { get; set; }

    /// <summary>
    /// The reason the model stopped generating further tokens
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }
}

/// <summary>
/// Represents a chunk in a streaming chat completion response
/// </summary>
public class ChatCompletionChunk : BaseModel
{
    /// <summary>
    /// The identifier for this chat completion chunk
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The object type, which is always "chat.completion.chunk"
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// The time when the chat completion chunk was created
    /// </summary>
    [JsonPropertyName("created")]
    public int Created { get; set; }

    /// <summary>
    /// The model used for the chat completion
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// The choices made by the model for this chunk
    /// </summary>
    [JsonPropertyName("choices")]
    public List<ChatCompletionChunkChoice> Choices { get; set; }
}