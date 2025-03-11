namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a chat message
/// </summary>
public class ChatMessage : BaseModel
{
    /// <summary>
    /// The role of the author of this message
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; }

    /// <summary>
    /// Creates a new chat message with the specified role and text content
    /// </summary>
    public static ChatMessage Create(string role, string content)
    {
        var message = new ChatMessage { Role = role };
        message.SetValue("content", content);
        return message;
    }

    /// <summary>
    /// Creates a new chat message with the specified role and structured content
    /// </summary>
    public static ChatMessage Create(string role, List<ChatMessageContent> content)
    {
        var message = new ChatMessage { Role = role };
        message.SetValue("content", content);
        return message;
    }

    /// <summary>
    /// Creates a new user message with text content
    /// </summary>
    public static ChatMessage FromUser(string content)
    {
        return Create(MessageRoles.User, content);
    }

    /// <summary>
    /// Creates a new developer message with text content
    /// </summary>
    public static ChatMessage FromDeveloper(string content)
    {
        return Create(MessageRoles.Developer, content);
    }

    /// <summary>
    /// Creates a new assistant message with text content
    /// </summary>
    public static ChatMessage FromAssistant(string content)
    {
        return Create(MessageRoles.Assistant, content);
    }

    /// <summary>
    /// Gets the content of the message
    /// </summary>
    public object GetContent()
    {
        return GetValue<object>("content");
    }

    /// <summary>
    /// Gets the content of the message as a string
    /// </summary>
    public string GetContentAsString()
    {
        var content = GetContent();
        return content?.ToString();
    }

    /// <summary>
    /// Gets the content of the message as a list of message content objects
    /// </summary>
    public List<ChatMessageContent> GetContentAsList()
    {
        return GetValue<List<ChatMessageContent>>("content");
    }
}