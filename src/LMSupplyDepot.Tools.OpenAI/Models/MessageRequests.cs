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
    /// A list of files attached to the message, and the tools they should be added to.
    /// </summary>
    [JsonPropertyName("attachments")]
    public List<MessageAttachment> Attachments { get; set; }

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
    public CreateMessageRequest WithAttachments(List<MessageAttachment> attachments)
    {
        Attachments = attachments;
        return this;
    }

    /// <summary>
    /// Adds a file attachment with file search capability to the message
    /// </summary>
    [Obsolete("This method is deprecated. Use Vector Store based approach instead.")]
    public CreateMessageRequest WithFileSearchAttachment(string fileId)
    {
        if (Attachments == null)
        {
            Attachments = new List<MessageAttachment>();
        }

        var attachment = MessageAttachment.Create(fileId).WithFileSearchTool();
        Attachments.Add(attachment);

        return this;
    }

    /// <summary>
    /// Adds multiple file attachments with file search capability to the message
    /// </summary>
    [Obsolete("This method is deprecated. Use Vector Store based approach instead.")]
    public CreateMessageRequest WithFileSearchAttachments(List<string> fileIds)
    {
        if (fileIds == null || fileIds.Count == 0)
            return this;

        if (Attachments == null)
        {
            Attachments = new List<MessageAttachment>();
        }

        foreach (var fileId in fileIds)
        {
            var attachment = MessageAttachment.Create(fileId).WithFileSearchTool();
            Attachments.Add(attachment);
        }

        return this;
    }

    /// <summary>
    /// Adds a file attachment with code interpreter capability to the message
    /// </summary>
    public CreateMessageRequest WithCodeInterpreterAttachment(string fileId)
    {
        if (Attachments == null)
        {
            Attachments = new List<MessageAttachment>();
        }

        var attachment = MessageAttachment.Create(fileId).WithCodeInterpreterTool();
        Attachments.Add(attachment);

        return this;
    }

    /// <summary>
    /// Adds multiple file attachments with code interpreter capability to the message
    /// </summary>
    public CreateMessageRequest WithCodeInterpreterAttachments(List<string> fileIds)
    {
        if (fileIds == null || fileIds.Count == 0)
            return this;

        if (Attachments == null)
        {
            Attachments = new List<MessageAttachment>();
        }

        foreach (var fileId in fileIds)
        {
            var attachment = MessageAttachment.Create(fileId).WithCodeInterpreterTool();
            Attachments.Add(attachment);
        }

        return this;
    }
}