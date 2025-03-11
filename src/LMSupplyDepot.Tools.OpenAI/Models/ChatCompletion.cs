namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a chat completion response
/// </summary>
public class ChatCompletion : BaseModel
{
    /// <summary>
    /// The identifier for this chat completion
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The object type, which is always "chat.completion"
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// The time when the chat completion was created
    /// </summary>
    [JsonPropertyName("created")]
    public int Created { get; set; }

    /// <summary>
    /// The model used for the chat completion
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// The choices made by the model
    /// </summary>
    [JsonPropertyName("choices")]
    public List<ChatCompletionChoice> Choices { get; set; }

    /// <summary>
    /// Gets the usage information for this chat completion
    /// </summary>
    public ChatCompletionUsage GetUsage()
    {
        return GetValue<ChatCompletionUsage>("usage");
    }
}

/// <summary>
/// Represents usage information for a chat completion
/// </summary>
public class ChatCompletionUsage : BaseModel
{
    /// <summary>
    /// Number of tokens in the prompt
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    /// <summary>
    /// Number of tokens in the generated completion
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total number of tokens used (prompt + completion)
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}