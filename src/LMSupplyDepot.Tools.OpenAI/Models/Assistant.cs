namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents an Assistant in the OpenAI Assistants API
/// </summary>
public class Assistant : MetadataResource
{
    /// <summary>
    /// The name of the assistant
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The description of the assistant
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; }

    /// <summary>
    /// ID of the model to use
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// The system instructions that the assistant uses
    /// </summary>
    [JsonPropertyName("instructions")]
    public string Instructions { get; set; }

    /// <summary>
    /// Get the tools associated with this assistant
    /// </summary>
    public List<Tool> GetTools()
    {
        return GetValue<List<Tool>>(PropertyNames.Tools);
    }

    /// <summary>
    /// Set the tools for this assistant
    /// </summary>
    public void SetTools(List<Tool> tools)
    {
        SetValue(PropertyNames.Tools, tools);
    }

    /// <summary>
    /// Get the tool resources for this assistant
    /// </summary>
    public object GetToolResources()
    {
        return GetValue<object>(PropertyNames.ToolResources);
    }

    /// <summary>
    /// Set the tool resources for this assistant
    /// </summary>
    public void SetToolResources(object toolResources)
    {
        SetValue(PropertyNames.ToolResources, toolResources);
    }
}