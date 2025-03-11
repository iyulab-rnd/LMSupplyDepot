namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a Message in the Assistants API
/// </summary>
public class Message : MetadataResource
{
    /// <summary>
    /// The thread ID that this message belongs to
    /// </summary>
    [JsonPropertyName("thread_id")]
    public string ThreadId { get; set; }

    /// <summary>
    /// The entity that produced the message. One of "user" or "assistant"
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; }

    /// <summary>
    /// The content of the message in different formats
    /// </summary>
    [JsonPropertyName("content")]
    public List<MessageContent> Content { get; set; }

    /// <summary>
    /// If applicable, the ID of the assistant that authored this message
    /// </summary>
    [JsonPropertyName("assistant_id")]
    public string AssistantId { get; set; }

    /// <summary>
    /// If applicable, the ID of the run associated with the authoring of this message
    /// </summary>
    [JsonPropertyName("run_id")]
    public string RunId { get; set; }

    /// <summary>
    /// A list of file IDs that the message has access to
    /// </summary>
    [JsonPropertyName("file_ids")]
    public List<string> FileIds { get; set; }

    /// <summary>
    /// Get the attachments of the message
    /// </summary>
    public List<object> GetAttachments()
    {
        return GetValue<List<object>>(PropertyNames.Attachments);
    }

    /// <summary>
    /// Set the attachments for this message
    /// </summary>
    public void SetAttachments(List<object> attachments)
    {
        SetValue(PropertyNames.Attachments, attachments);
    }
}