namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Class representing the structure of a message delta event
/// </summary>
public class MessageDelta : BaseModel
{
    /// <summary>
    /// Message ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Object type (always "thread.message.delta")
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// Delta content
    /// </summary>
    [JsonPropertyName("delta")]
    public MessageDeltaContent Delta { get; set; }
}

/// <summary>
/// Class representing message delta content
/// </summary>
public class MessageDeltaContent : BaseModel
{
    /// <summary>
    /// Content changes
    /// </summary>
    [JsonPropertyName("content")]
    public List<MessageContentDelta> Content { get; set; }

    /// <summary>
    /// Role changes
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; }
}

/// <summary>
/// Class representing a message content delta
/// </summary>
public class MessageContentDelta : BaseModel
{
    /// <summary>
    /// Delta index
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// Delta type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Text content (if type is "text")
    /// </summary>
    [JsonPropertyName("text")]
    public TextDelta Text { get; set; }
}

/// <summary>
/// Class representing a text delta
/// </summary>
public class TextDelta : BaseModel
{
    /// <summary>
    /// Text value
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; }

    /// <summary>
    /// Text annotations
    /// </summary>
    [JsonPropertyName("annotations")]
    public List<Annotation> Annotations { get; set; }
}