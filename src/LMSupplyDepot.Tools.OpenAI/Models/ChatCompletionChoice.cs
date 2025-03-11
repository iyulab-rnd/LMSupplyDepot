namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a choice in a chat completion response
/// </summary>
public class ChatCompletionChoice : BaseModel
{
    /// <summary>
    /// The index of the choice
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// The message for this choice
    /// </summary>
    [JsonPropertyName("message")]
    public ChatMessage Message { get; set; }

    /// <summary>
    /// The reason the model stopped generating further tokens
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }

    /// <summary>
    /// Gets tool calls from the message if any
    /// </summary>
    public List<ToolCall> GetToolCalls()
    {
        if (Message == null)
            return null;

        return Message.GetValue<List<ToolCall>>("tool_calls");
    }

    /// <summary>
    /// Checks if the completion has tool calls
    /// </summary>
    public bool HasToolCalls()
    {
        return FinishReason == FinishReasons.ToolCalls && GetToolCalls() != null;
    }
}