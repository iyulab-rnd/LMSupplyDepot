namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a file attached to an assistant
/// </summary>
public class AssistantFile : OpenAIResource
{
    /// <summary>
    /// The ID of the assistant that the file is attached to
    /// </summary>
    [JsonPropertyName("assistant_id")]
    public string AssistantId { get; set; }
}

/// <summary>
/// Represents a file attached to a message
/// </summary>
public class MessageFile : OpenAIResource
{
    /// <summary>
    /// The ID of the message that the file is attached to
    /// </summary>
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }
}