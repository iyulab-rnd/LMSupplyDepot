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
    /// Creates a new message attachment with the specified file ID
    /// </summary>
    public static MessageAttachment Create(string fileId)
    {
        return new MessageAttachment { FileId = fileId };
    }

    /// <summary>
    /// Sets the tools for this attachment
    /// </summary>
    public MessageAttachment WithTools(List<Tool> tools)
    {
        SetValue("tools", tools);
        return this;
    }

    /// <summary>
    /// Sets the file search tool for this attachment
    /// </summary>
    public MessageAttachment WithFileSearchTool()
    {
        var tools = new List<Tool> { Tool.CreateFileSearchTool() };
        return WithTools(tools);
    }
}