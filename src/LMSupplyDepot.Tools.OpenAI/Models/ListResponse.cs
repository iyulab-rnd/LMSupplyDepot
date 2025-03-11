namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a list response in the OpenAI API
/// </summary>
/// <typeparam name="T">The type of objects in the list</typeparam>
public class ListResponse<T> : BaseModel
{
    /// <summary>
    /// The object type, which is always "list"
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// The list of objects
    /// </summary>
    [JsonPropertyName("data")]
    public List<T> Data { get; set; }

    /// <summary>
    /// The ID of the first object in the list
    /// </summary>
    [JsonPropertyName("first_id")]
    public string FirstId { get; set; }

    /// <summary>
    /// The ID of the last object in the list
    /// </summary>
    [JsonPropertyName("last_id")]
    public string LastId { get; set; }

    /// <summary>
    /// Whether there are more objects available
    /// </summary>
    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }
}