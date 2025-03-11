namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents information about an AI model
/// </summary>
public class ModelInfo : BaseModel
{
    /// <summary>
    /// The identifier of the model
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The name of the model
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The family or category this model belongs to
    /// </summary>
    [JsonPropertyName("family")]
    public string Family { get; set; }

    /// <summary>
    /// A description of the model's capabilities
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; }

    /// <summary>
    /// Maximum number of tokens in the context window
    /// </summary>
    [JsonPropertyName("context_window")]
    public int ContextWindow { get; set; }

    /// <summary>
    /// Maximum number of output tokens the model can generate
    /// </summary>
    [JsonPropertyName("max_output_tokens")]
    public int MaxOutputTokens { get; set; }

    /// <summary>
    /// The date this version of the model was released
    /// </summary>
    [JsonPropertyName("release_date")]
    public DateTime? ReleaseDate { get; set; }

    /// <summary>
    /// The model's knowledge cutoff date
    /// </summary>
    [JsonPropertyName("knowledge_cutoff")]
    public DateTime? KnowledgeCutoff { get; set; }

    /// <summary>
    /// Whether this is an alias to another model
    /// </summary>
    [JsonPropertyName("is_alias")]
    public bool IsAlias { get; set; }

    /// <summary>
    /// If this is an alias, the ID of the actual model it points to
    /// </summary>
    [JsonPropertyName("points_to")]
    public string PointsTo { get; set; }
}

/// <summary>
/// Response model for listing models
/// </summary>
public class ListModelsResponse : BaseModel
{
    /// <summary>
    /// The object type, which is always "list"
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// The list of model objects
    /// </summary>
    [JsonPropertyName("data")]
    public List<ModelInfo> Data { get; set; }
}