namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Response format for structured JSON outputs
/// </summary>
public class JsonResponseFormat : BaseModel
{
    /// <summary>
    /// Creates a new JSON response format
    /// </summary>
    public static JsonResponseFormat Create(object? schema = null)
    {
        var format = new JsonResponseFormat();
        format.SetValue("type", "json_schema");

        if (schema != null)
        {
            format.SetValue("json_schema", schema);
        }

        return format;
    }
}