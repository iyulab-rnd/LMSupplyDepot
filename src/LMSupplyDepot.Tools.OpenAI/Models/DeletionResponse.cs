namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a response from a deletion operation
/// </summary>
public class DeletionResponse : BaseModel
{
    /// <summary>
    /// The ID of the deleted object
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The type of object that was deleted
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// Whether the deletion was successful
    /// </summary>
    [JsonPropertyName("deleted")]
    public bool Deleted { get; set; }
}