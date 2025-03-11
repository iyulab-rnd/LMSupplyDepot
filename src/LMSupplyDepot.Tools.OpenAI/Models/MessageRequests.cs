namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Request model for creating a Message
/// </summary>
public class CreateMessageRequest : BaseRequest
{
    /// <summary>
    /// The role of the entity creating the message. Currently only "user" is supported.
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    /// <summary>
    /// The content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; }

    /// <summary>
    /// Creates a new CreateMessageRequest with the specified content
    /// </summary>
    public static CreateMessageRequest Create(string content)
    {
        return new CreateMessageRequest { Content = content };
    }

    /// <summary>
    /// Sets the file IDs for the message
    /// </summary>
    public CreateMessageRequest WithFileIds(List<string> fileIds)
    {
        SetValue(PropertyNames.FileIds, fileIds);
        return this;
    }

    /// <summary>
    /// Sets the metadata for the message
    /// </summary>
    public CreateMessageRequest WithMetadata(Dictionary<string, string> metadata)
    {
        SetValue(PropertyNames.Metadata, metadata);
        return this;
    }

    /// <summary>
    /// Sets the attachments for the message
    /// </summary>
    public CreateMessageRequest WithAttachments(List<object> attachments)
    {
        SetValue(PropertyNames.Attachments, attachments);
        return this;
    }
}