namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Class representing an error event
/// </summary>
public class ErrorEvent : BaseModel
{
    /// <summary>
    /// Error message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; }

    /// <summary>
    /// Error type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Error code
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; }
}