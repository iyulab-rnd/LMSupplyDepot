namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a vector store file batch
/// </summary>
public class VectorStoreFileBatch : OpenAIResource
{
    /// <summary>
    /// The ID of the vector store that the file batch belongs to
    /// </summary>
    [JsonPropertyName("vector_store_id")]
    public string VectorStoreId { get; set; }

    /// <summary>
    /// The status of the vector store file batch
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// The file counts in this batch
    /// </summary>
    [JsonPropertyName("file_counts")]
    public VectorStoreFileCounts FileCounts { get; set; }
}