namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a Run in the Assistants API
/// </summary>
public class Run : AsyncOperationResource
{
    /// <summary>
    /// The ID of the thread that was executed on as a part of this run
    /// </summary>
    [JsonPropertyName("thread_id")]
    public string ThreadId { get; set; }

    /// <summary>
    /// The ID of the assistant used for execution of this run
    /// </summary>
    [JsonPropertyName("assistant_id")]
    public string AssistantId { get; set; }

    /// <summary>
    /// The model that the assistant used for this run
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// The instructions that the assistant used for this run
    /// </summary>
    [JsonPropertyName("instructions")]
    public string Instructions { get; set; }

    /// <summary>
    /// Get the required action for this run
    /// </summary>
    public RequiredAction GetRequiredAction()
    {
        return GetValue<RequiredAction>(PropertyNames.RequiredAction);
    }

    /// <summary>
    /// Get the tools for this run
    /// </summary>
    public List<Tool> GetTools()
    {
        return GetValue<List<Tool>>(PropertyNames.Tools);
    }

    /// <summary>
    /// Get the file IDs for this run
    /// </summary>
    public List<string> GetFileIds()
    {
        return GetValue<List<string>>(PropertyNames.FileIds);
    }

    /// <summary>
    /// Get the last error for this run
    /// </summary>
    public object GetLastError()
    {
        return GetValue<object>(PropertyNames.LastError);
    }
}