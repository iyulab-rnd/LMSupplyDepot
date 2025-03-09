namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a request to create embeddings
/// </summary>
public class CreateEmbeddingsRequest : BaseRequest
{
    /// <summary>
    /// ID of the model to use
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// The text to generate embeddings for
    /// </summary>
    [JsonPropertyName("input")]
    public object Input { get; set; }

    /// <summary>
    /// The encoding format to use for the embeddings
    /// </summary>
    [JsonPropertyName("encoding_format")]
    public string EncodingFormat { get; set; }

    /// <summary>
    /// Creates a new CreateEmbeddingsRequest with the specified model and input
    /// </summary>
    public static CreateEmbeddingsRequest Create(string model, string input)
    {
        return new CreateEmbeddingsRequest
        {
            Model = model,
            Input = input
        };
    }

    /// <summary>
    /// Creates a new CreateEmbeddingsRequest with the specified model and inputs
    /// </summary>
    public static CreateEmbeddingsRequest Create(string model, List<string> inputs)
    {
        return new CreateEmbeddingsRequest
        {
            Model = model,
            Input = inputs
        };
    }

    /// <summary>
    /// Sets the encoding format for the embeddings
    /// </summary>
    public CreateEmbeddingsRequest WithEncodingFormat(string encodingFormat)
    {
        EncodingFormat = encodingFormat;
        return this;
    }

    /// <summary>
    /// Sets the dimensions for the embeddings
    /// </summary>
    public CreateEmbeddingsRequest WithDimensions(int dimensions)
    {
        SetValue("dimensions", dimensions);
        return this;
    }

    /// <summary>
    /// Sets a user identifier for the request
    /// </summary>
    public CreateEmbeddingsRequest WithUser(string user)
    {
        SetValue("user", user);
        return this;
    }
}

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