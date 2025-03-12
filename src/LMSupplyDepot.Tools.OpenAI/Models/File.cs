namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a file in the OpenAI API
/// </summary>
public class File : MetadataResource
{
    /// <summary>
    /// The size of the file in bytes
    /// </summary>
    [JsonPropertyName("bytes")]
    public long Bytes { get; set; }

    /// <summary>
    /// The time when the file was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public int CreatedAt { get; set; }

    /// <summary>
    /// The filename of the file
    /// </summary>
    [JsonPropertyName("filename")]
    public string Filename { get; set; }

    /// <summary>
    /// The purpose of the file
    /// </summary>
    [JsonPropertyName("purpose")]
    public string Purpose { get; set; }

    /// <summary>
    /// The status of the file
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// For assistants/vision/audio files, the corresponding file URL will be returned if status is processed
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; }
}