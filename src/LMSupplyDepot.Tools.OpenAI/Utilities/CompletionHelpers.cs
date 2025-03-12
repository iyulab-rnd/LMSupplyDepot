namespace LMSupplyDepot.Tools.OpenAI.Utilities;

/// <summary>
/// Helper methods for working with chat completions
/// </summary>
public static class CompletionHelpers
{
    /// <summary>
    /// Creates a JSON response format for structured outputs
    /// </summary>
    public static JsonResponseFormat CreateJsonResponseFormat(object? schema = null)
    {
        return JsonResponseFormat.Create(schema);
    }

    /// <summary>
    /// Extracts the plain text content from a completion response
    /// </summary>
    public static string ExtractTextContent(ChatCompletion completion)
    {
        if (completion?.Choices == null || completion.Choices.Count == 0)
        {
            return string.Empty;
        }

        return completion.Choices[0].Message.GetContentAsString();
    }

    /// <summary>
    /// Tries to parse a completion response as a specific type
    /// </summary>
    public static bool TryParseCompletionAs<T>(ChatCompletion completion, out T result) where T : class
    {
        result = default;

        if (completion?.Choices == null || completion.Choices.Count == 0)
        {
            return false;
        }

        var content = completion.Choices[0].Message.GetContentAsString();
        if (string.IsNullOrEmpty(content))
        {
            return false;
        }

        try
        {
            result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return result != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parses a completion response as a specific type
    /// </summary>
    public static T ParseCompletionAs<T>(ChatCompletion completion) where T : class
    {
        if (completion?.Choices == null || completion.Choices.Count == 0)
        {
            throw new ArgumentException("Completion has no choices", nameof(completion));
        }

        var content = completion.Choices[0].Message.GetContentAsString();
        if (string.IsNullOrEmpty(content))
        {
            throw new ArgumentException("Completion message content is empty", nameof(completion));
        }

        try
        {
            var result = JsonSerializer.Deserialize<T>(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (result == null)
            {
                throw new JsonException("Deserialized result is null");
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Failed to parse completion as {typeof(T).Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Builds a conversation history from alternating user and assistant messages
    /// </summary>
    public static List<ChatMessage> BuildConversationHistory(params string[] messages)
    {
        if (messages == null || messages.Length == 0)
        {
            return new List<ChatMessage>();
        }

        var chatMessages = new List<ChatMessage>();
        for (int i = 0; i < messages.Length; i++)
        {
            string role = i % 2 == 0 ? MessageRoles.User : MessageRoles.Assistant;
            chatMessages.Add(ChatMessage.Create(role, messages[i]));
        }

        return chatMessages;
    }

    /// <summary>
    /// Extracts tool calls from a completion response and maps them to a more convenient format
    /// </summary>
    public static List<(string Id, string Type, string FunctionName, string Arguments)> GetFormattedToolCalls(ChatCompletion completion)
    {
        if (completion?.Choices == null || completion.Choices.Count == 0)
        {
            return new List<(string, string, string, string)>();
        }

        var toolCalls = completion.Choices[0].GetToolCalls();
        if (toolCalls == null)
        {
            return new List<(string, string, string, string)>();
        }

        var result = new List<(string, string, string, string)>();

        foreach (var toolCall in toolCalls)
        {
            var function = toolCall.GetFunction();
            if (function != null)
            {
                result.Add((toolCall.Id, toolCall.Type, function["name"].ToString(), function["arguments"].ToString()));
            }
        }

        return result;
    }
}