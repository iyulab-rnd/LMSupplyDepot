namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a message attachment
/// </summary>
public class MessageAttachment : BaseModel
{
    /// <summary>
    /// The ID of the file to attach
    /// </summary>
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }

    /// <summary>
    /// The tools to add this file to
    /// </summary>
    [JsonPropertyName("tools")]
    public List<AttachmentTool> Tools { get; set; }

    /// <summary>
    /// Creates a new message attachment with the specified file ID
    /// </summary>
    public static MessageAttachment Create(string fileId)
    {
        return new MessageAttachment { FileId = fileId };
    }

    /// <summary>
    /// Sets the tools for this attachment
    /// </summary>
    public MessageAttachment WithTools(List<AttachmentTool> tools)
    {
        Tools = tools;
        return this;
    }

    /// <summary>
    /// Adds the file search tool to this attachment
    /// </summary>
    public MessageAttachment WithFileSearchTool()
    {
        if (Tools == null)
        {
            Tools = new List<AttachmentTool>();
        }

        Tools.Add(AttachmentTool.CreateFileSearchTool());
        return this;
    }

    /// <summary>
    /// Adds the code interpreter tool to this attachment
    /// </summary>
    public MessageAttachment WithCodeInterpreterTool()
    {
        if (Tools == null)
        {
            Tools = new List<AttachmentTool>();
        }

        Tools.Add(AttachmentTool.CreateCodeInterpreterTool());
        return this;
    }
}

/// <summary>
/// Represents a tool definition for an attachment
/// </summary>
public class AttachmentTool : BaseModel
{
    /// <summary>
    /// The type of tool being defined
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Creates a file search tool
    /// </summary>
    public static AttachmentTool CreateFileSearchTool()
    {
        return new AttachmentTool { Type = ToolTypes.FileSearch };
    }

    /// <summary>
    /// Creates a code interpreter tool
    /// </summary>
    public static AttachmentTool CreateCodeInterpreterTool()
    {
        return new AttachmentTool { Type = ToolTypes.CodeInterpreter };
    }
}