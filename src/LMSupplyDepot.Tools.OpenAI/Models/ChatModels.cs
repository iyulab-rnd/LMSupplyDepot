using System.Text.Json.Serialization;

namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Constants for message roles in chat completions
/// </summary>
public static class MessageRoles
{
    public const string User = "user";
    public const string Assistant = "assistant";
    public const string Developer = "developer";
    public const string Function = "function";
}

/// <summary>
/// Constants for content types
/// </summary>
public static class ContentTypes
{
    public const string Text = "text";
    public const string ImageUrl = "image_url";
}

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

/// <summary>
/// Represents a choice in a chat completion response
/// </summary>
public class ChatCompletionChoice : BaseModel
{
    /// <summary>
    /// The index of the choice
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// The message for this choice
    /// </summary>
    [JsonPropertyName("message")]
    public ChatMessage Message { get; set; }

    /// <summary>
    /// The reason the model stopped generating further tokens
    /// </summary>
    [JsonPropertyName("finish_reason")]
    public string FinishReason { get; set; }

    /// <summary>
    /// Gets tool calls from the message if any
    /// </summary>
    public List<ToolCall> GetToolCalls()
    {
        if (Message == null)
            return null;

        return Message.GetValue<List<ToolCall>>("tool_calls");
    }

    /// <summary>
    /// Checks if the completion has tool calls
    /// </summary>
    public bool HasToolCalls()
    {
        return FinishReason == FinishReasons.ToolCalls && GetToolCalls() != null;
    }
}

/// <summary>
/// Represents a chat completion response
/// </summary>
public class ChatCompletion : BaseModel
{
    /// <summary>
    /// The identifier for this chat completion
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The object type, which is always "chat.completion"
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// The time when the chat completion was created
    /// </summary>
    [JsonPropertyName("created")]
    public int Created { get; set; }

    /// <summary>
    /// The model used for the chat completion
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// The choices made by the model
    /// </summary>
    [JsonPropertyName("choices")]
    public List<ChatCompletionChoice> Choices { get; set; }

    /// <summary>
    /// Gets the usage information for this chat completion
    /// </summary>
    public ChatCompletionUsage GetUsage()
    {
        return GetValue<ChatCompletionUsage>("usage");
    }
}

/// <summary>
/// Represents usage information for a chat completion
/// </summary>
public class ChatCompletionUsage : BaseModel
{
    /// <summary>
    /// Number of tokens in the prompt
    /// </summary>
    [JsonPropertyName("prompt_tokens")]
    public int PromptTokens { get; set; }

    /// <summary>
    /// Number of tokens in the generated completion
    /// </summary>
    [JsonPropertyName("completion_tokens")]
    public int CompletionTokens { get; set; }

    /// <summary>
    /// Total number of tokens used (prompt + completion)
    /// </summary>
    [JsonPropertyName("total_tokens")]
    public int TotalTokens { get; set; }
}