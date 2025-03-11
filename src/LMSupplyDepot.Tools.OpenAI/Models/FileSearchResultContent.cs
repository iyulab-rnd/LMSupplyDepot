namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents file search result content in a run step
/// </summary>
public class FileSearchResultContent : BaseModel
{
    /// <summary>
    /// The content of the search result
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; }

    /// <summary>
    /// The file ID of the content
    /// </summary>
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }

    /// <summary>
    /// Gets the score of the search result if available
    /// </summary>
    public double? GetScore()
    {
        return GetValue<double?>("score");
    }
}