namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a tool call in a completion response
/// </summary>
public class ToolCall : BaseModel
{
    /// <summary>
    /// The ID of the tool call
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The type of the tool call
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Gets the function call details
    /// </summary>
    public Dictionary<string, object> GetFunction()
    {
        return GetValue<Dictionary<string, object>>("function");
    }

    /// <summary>
    /// Gets the function name
    /// </summary>
    public string GetFunctionName()
    {
        var function = GetFunction();
        return function != null && function.ContainsKey("name") ? function["name"].ToString() : null;
    }

    /// <summary>
    /// Gets the function arguments as a string
    /// </summary>
    public string GetFunctionArguments()
    {
        var function = GetFunction();
        return function != null && function.ContainsKey("arguments") ? function["arguments"].ToString() : null;
    }
}