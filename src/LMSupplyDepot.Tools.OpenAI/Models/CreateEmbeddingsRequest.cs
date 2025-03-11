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