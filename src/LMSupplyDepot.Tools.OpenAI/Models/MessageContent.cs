namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents the content of a message
/// </summary>
public class MessageContent : BaseModel
{
    /// <summary>
    /// The type of content. Can be "text", "image_file", etc.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("text")]
    public TextContent? Text { get; set; }


    [JsonPropertyName("image_file")]
    public ImageFileContent? ImageFile { get; set; }
}

/// <summary>
/// Represents text content
/// </summary>
public class TextContent : BaseModel
{
    /// <summary>
    /// The text value
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; }

    /// <summary>
    /// Get the annotations within the text
    /// </summary>
    public List<Annotation> GetAnnotations()
    {
        return GetValue<List<Annotation>>("annotations");
    }
}

/// <summary>
/// Represents an annotation within text content
/// </summary>
public class Annotation : BaseModel
{
    /// <summary>
    /// Type of annotation
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// The text that is being annotated
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; }

    /// <summary>
    /// Start index of the annotation in the text
    /// </summary>
    [JsonPropertyName("start_index")]
    public int StartIndex { get; set; }

    /// <summary>
    /// End index of the annotation in the text
    /// </summary>
    [JsonPropertyName("end_index")]
    public int EndIndex { get; set; }
}

/// <summary>
/// Represents an image file content
/// </summary>
public class ImageFileContent : BaseModel
{
    /// <summary>
    /// The ID of the image file
    /// </summary>
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }
}