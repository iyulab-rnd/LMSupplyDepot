namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a vector store file
/// </summary>
public class VectorStoreFile : OpenAIResource
{
    /// <summary>
    /// The total vector store usage in bytes
    /// </summary>
    [JsonPropertyName("usage_bytes")]
    public long? UsageBytes { get; set; }

    /// <summary>
    /// The ID of the vector store that the file is attached to
    /// </summary>
    [JsonPropertyName("vector_store_id")]
    public string VectorStoreId { get; set; }

    /// <summary>
    /// The status of the vector store file
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// The ID of the file in this vector store
    /// </summary>
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }

    /// <summary>
    /// Gets the last error associated with this vector store file
    /// </summary>
    public VectorStoreFileError GetLastError()
    {
        return GetValue<VectorStoreFileError>("last_error");
    }

    /// <summary>
    /// Gets the chunking strategy used for this file
    /// </summary>
    public ChunkingStrategy GetChunkingStrategy()
    {
        return GetValue<ChunkingStrategy>("chunking_strategy");
    }
}

/// <summary>
/// Represents a vector store file error
/// </summary>
public class VectorStoreFileError : BaseModel
{
    /// <summary>
    /// The error code
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; }

    /// <summary>
    /// A human-readable description of the error
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; }
}