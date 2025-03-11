namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a piece of content in a message for Chat API
/// </summary>
public class ChatMessageContent : BaseModel
{
    /// <summary>
    /// The type of content
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Creates a text content
    /// </summary>
    public static ChatMessageContent Text(string text)
    {
        var content = new ChatMessageContent { Type = ContentTypes.Text };
        content.SetValue("text", text);
        return content;
    }

    /// <summary>
    /// Creates an image URL content
    /// </summary>
    public static ChatMessageContent ImageUrl(string url)
    {
        var content = new ChatMessageContent { Type = ContentTypes.ImageUrl };
        var imageUrl = new Dictionary<string, string> { { "url", url } };
        content.SetValue("image_url", imageUrl);
        return content;
    }

    /// <summary>
    /// Gets the text if this is a text content
    /// </summary>
    public string GetText()
    {
        return GetValue<string>("text");
    }

    /// <summary>
    /// Gets the image URL if this is an image URL content
    /// </summary>
    public string GetImageUrl()
    {
        var imageUrl = GetValue<Dictionary<string, string>>("image_url");
        return imageUrl?.GetValueOrDefault("url");
    }
}