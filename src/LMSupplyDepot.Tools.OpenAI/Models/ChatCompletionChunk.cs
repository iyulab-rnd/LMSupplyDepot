namespace LMSupplyDepot.Tools.OpenAI.Models;

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
        // Return Content property directly
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