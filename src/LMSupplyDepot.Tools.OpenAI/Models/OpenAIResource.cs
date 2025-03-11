
namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Interface for OpenAI resources with common properties
/// </summary>
public interface IOpenAIResource
{
    string Id { get; set; }
    string Object { get; set; }
    int CreatedAt { get; set; }
}

/// <summary>
/// Interface for resources that can have metadata
/// </summary>
public interface IMetadataContainer
{
    Dictionary<string, string> Metadata { get; set; }
}

/// <summary>
/// Base class for OpenAI API resources with common properties
/// </summary>
public class OpenAIResource : BaseModel, IOpenAIResource
{
    /// <summary>
    /// The identifier, which can be referenced in API endpoints
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The object type
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// The Unix timestamp (in seconds) for when the object was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public int CreatedAt { get; set; }
}

/// <summary>
/// Base class for OpenAI API resources that can have metadata
/// </summary>
public class MetadataResource : OpenAIResource, IMetadataContainer
{
    /// <summary>
    /// Set of key-value pairs that can be attached to the object
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; }
}

/// <summary>
/// Base class for OpenAI API resources that represent asynchronous operations
/// </summary>
public class AsyncOperationResource : MetadataResource
{
    /// <summary>
    /// The status of the operation
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// The time when the operation was started
    /// </summary>
    [JsonPropertyName("started_at")]
    public int? StartedAt { get; set; }

    /// <summary>
    /// The time when the operation was completed
    /// </summary>
    [JsonPropertyName("completed_at")]
    public int? CompletedAt { get; set; }

    /// <summary>
    /// The time when the operation failed, if applicable
    /// </summary>
    [JsonPropertyName("failed_at")]
    public int? FailedAt { get; set; }

    /// <summary>
    /// The time when the operation was cancelled, if applicable
    /// </summary>
    [JsonPropertyName("cancelled_at")]
    public int? CancelledAt { get; set; }

    /// <summary>
    /// The time when the operation expires, if applicable
    /// </summary>
    [JsonPropertyName("expires_at")]
    public int? ExpiresAt { get; set; }
}