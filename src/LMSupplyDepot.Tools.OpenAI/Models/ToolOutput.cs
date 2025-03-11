namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a tool output for submitting to a run
/// </summary>
public class ToolOutput : BaseModel
{
    /// <summary>
    /// The ID of the tool call
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    public string ToolCallId { get; set; }

    /// <summary>
    /// The output of the tool call
    /// </summary>
    [JsonPropertyName("output")]
    public string Output { get; set; }

    /// <summary>
    /// Creates a new ToolOutput with the specified tool call ID and output
    /// </summary>
    public static ToolOutput Create(string toolCallId, string output)
    {
        return new ToolOutput
        {
            ToolCallId = toolCallId,
            Output = output
        };
    }
}