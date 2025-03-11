namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a Thread in the Assistants API
/// </summary>
public class ChatThread : MetadataResource
{
    /// <summary>
    /// Get the tool resources for this thread
    /// </summary>
    public object GetToolResources()
    {
        return GetValue<object>(PropertyNames.ToolResources);
    }

    /// <summary>
    /// Set the tool resources for this thread
    /// </summary>
    public void SetToolResources(object toolResources)
    {
        SetValue(PropertyNames.ToolResources, toolResources);
    }
}