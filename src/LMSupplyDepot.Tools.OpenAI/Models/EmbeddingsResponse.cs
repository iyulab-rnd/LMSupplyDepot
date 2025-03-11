namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents an embedding data item in an embeddings response
/// </summary>
public class EmbeddingData : BaseModel
{
    /// <summary>
    /// The object type, which is always "embedding"
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// The embedding vector
    /// </summary>
    [JsonPropertyName("embedding")]
    public List<float> Embedding { get; set; }

    /// <summary>
    /// The index of this embedding
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }
}

/// <summary>
/// Represents an embeddings response
/// </summary>
public class EmbeddingsResponse : BaseModel
{
    /// <summary>
    /// The object type, which is always "list"
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// The list of embedding data items
    /// </summary>
    [JsonPropertyName("data")]
    public List<EmbeddingData> Data { get; set; }

    /// <summary>
    /// The model used for the embeddings
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// Gets the usage information for this embeddings response
    /// </summary>
    public EmbeddingsUsage GetUsage()
    {
        return GetValue<EmbeddingsUsage>("usage");
    }
}

/// <summary>
/// Represents usage information for an embeddings response
/// </summary>
public class EmbeddingsUsage : BaseModel
{
    /// <summary>
    /// Number of tokens in the prompt
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    /// <summary>
    /// Total number of tokens used
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}