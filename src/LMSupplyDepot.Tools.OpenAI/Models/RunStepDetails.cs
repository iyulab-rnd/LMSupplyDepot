using System.Text.Json;

namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents the details of a run step
/// </summary>
public class RunStepDetails : BaseModel
{
    /// <summary>
    /// The type of step
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// The message creation details if this is a message_creation step
    /// </summary>
    [JsonPropertyName("message_creation")]
    public MessageCreationDetails MessageCreation { get; set; }

    /// <summary>
    /// The tool calls details if this is a tool_calls step
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public List<RunToolCall> ToolCalls { get; set; } = new List<RunToolCall>();
}

/// <summary>
/// Represents message creation details in a run step
/// </summary>
public class MessageCreationDetails : BaseModel
{
    /// <summary>
    /// The ID of the message that was created
    /// </summary>
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }
}

/// <summary>
/// Extension methods for RunStep
/// </summary>
public static class RunStepExtensions
{
    /// <summary>
    /// Gets the message ID if this is a message creation step
    /// </summary>
    public static string GetCreatedMessageId(this RunStep step)
    {
        var details = step.GetStepDetails();
        if (details == null || details.Type != "message_creation" || details.MessageCreation == null)
            return null;

        return details.MessageCreation.MessageId;
    }

    /// <summary>
    /// Gets the tool calls if this is a tool calls step
    /// </summary>
    public static List<RunToolCall> GetToolCalls(this RunStep step)
    {
        var details = step.GetStepDetails();
        if (details == null || details.Type != "tool_calls")
            return new List<RunToolCall>();

        return details.ToolCalls ?? new List<RunToolCall>();
    }
}