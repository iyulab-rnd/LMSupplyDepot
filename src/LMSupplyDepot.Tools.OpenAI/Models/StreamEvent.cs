namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Base class representing the structure of a streaming event
/// </summary>
public class StreamEvent : BaseModel
{
    /// <summary>
    /// Event type
    /// </summary>
    [JsonPropertyName("event")]
    public string Event { get; set; }

    /// <summary>
    /// Event data
    /// </summary>
    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }

    /// <summary>
    /// Convert data to a specific type
    /// </summary>
    public T GetDataAs<T>() where T : class
    {
        return JsonSerializer.Deserialize<T>(Data.GetRawText(), _jsonOptions);
    }

    // JSON options for deserialization
    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}