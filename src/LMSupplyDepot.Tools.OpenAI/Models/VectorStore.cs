namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a Vector Store in the Assistants API
/// </summary>
public class VectorStore : OpenAIResource
{
    /// <summary>
    /// The name of the vector store
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The status of the vector store
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// The size of the vector store in bytes
    /// </summary>
    [JsonPropertyName("bytes")]
    public long Bytes { get; set; }

    /// <summary>
    /// Gets the file counts for this vector store
    /// </summary>
    public VectorStoreFileCounts GetFileCounts()
    {
        return GetValue<VectorStoreFileCounts>("file_counts");
    }

    /// <summary>
    /// Gets the expiration policy for this vector store
    /// </summary>
    public object GetExpiresAfter()
    {
        return GetValue<object>("expires_after");
    }
}

/// <summary>
/// Represents file counts in a vector store
/// </summary>
public class VectorStoreFileCounts : BaseModel
{
    /// <summary>
    /// The number of files that are currently being processed
    /// </summary>
    [JsonPropertyName("in_progress")]
    public int InProgress { get; set; }

    /// <summary>
    /// The number of files that have been successfully processed
    /// </summary>
    [JsonPropertyName("completed")]
    public int Completed { get; set; }

    /// <summary>
    /// The number of files that have failed to process
    /// </summary>
    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    /// <summary>
    /// The number of files that were cancelled
    /// </summary>
    [JsonPropertyName("cancelled")]
    public int Cancelled { get; set; }

    /// <summary>
    /// The total number of files
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }
}