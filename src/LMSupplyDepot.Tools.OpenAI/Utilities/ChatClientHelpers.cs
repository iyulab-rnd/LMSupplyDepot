namespace LMSupplyDepot.Tools.OpenAI.Utilities;

/// <summary>
/// Helper methods for working with the OpenAI Chat API
/// </summary>
public static class ChatClientHelpers
{
    /// <summary>
    /// Creates a JSON response format for structured outputs
    /// </summary>
    public static object CreateJsonResponseFormat(object schema = null)
    {
        var format = new Dictionary<string, object>
        {
            { "type", "json_schema" }
        };

        if (schema != null)
        {
            format["json_schema"] = schema;
        }

        return format;
    }

    /// <summary>
    /// Creates a message with both text and image content
    /// </summary>
    public static ChatMessage CreateMultimodalMessage(string text, string imageUrl, string role = MessageRoles.User)
    {
        var content = new List<ChatMessageContent>
        {
            ChatMessageContent.Text(text),
            ChatMessageContent.ImageUrl(imageUrl)
        };

        return ChatMessage.Create(role, content);
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
    /// Calculates the cosine similarity between two embedding vectors
    /// </summary>
    public static double CosineSimilarity(List<float> embedding1, List<float> embedding2)
    {
        if (embedding1 == null || embedding2 == null || embedding1.Count != embedding2.Count)
        {
            throw new ArgumentException("Embeddings must be non-null and of the same length");
        }

        float dotProduct = 0;
        float norm1 = 0;
        float norm2 = 0;

        for (int i = 0; i < embedding1.Count; i++)
        {
            dotProduct += embedding1[i] * embedding2[i];
            norm1 += embedding1[i] * embedding1[i];
            norm2 += embedding2[i] * embedding2[i];
        }

        if (norm1 <= 0 || norm2 <= 0)
        {
            return 0;
        }

        return dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
    }

    /// <summary>
    /// Normalizes an embedding vector to unit length
    /// </summary>
    public static List<float> NormalizeEmbedding(List<float> embedding)
    {
        if (embedding == null || embedding.Count == 0)
        {
            return embedding;
        }

        float norm = 0;
        foreach (var value in embedding)
        {
            norm += value * value;
        }
        norm = (float)Math.Sqrt(norm);

        if (norm <= 0)
        {
            return embedding;
        }

        var normalized = new List<float>(embedding.Count);
        foreach (var value in embedding)
        {
            normalized.Add(value / norm);
        }

        return normalized;
    }

    /// <summary>
    /// Truncates an embedding vector to a specific number of dimensions
    /// </summary>
    public static List<float> TruncateEmbedding(List<float> embedding, int dimensions)
    {
        if (embedding == null || embedding.Count <= dimensions)
        {
            return embedding;
        }

        return embedding.GetRange(0, dimensions);
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
    /// Creates a chat message with a simple function call
    /// </summary>
    public static ChatMessage CreateFunctionCallMessage(string name, string arguments)
    {
        var message = new ChatMessage { Role = MessageRoles.Function };
        message.SetValue("name", name);
        message.SetValue("content", arguments);
        return message;
    }

    /// <summary>
    /// Creates a tool definition for function calling
    /// </summary>
    public static Tool CreateFunctionTool(string name, string description, Dictionary<string, object> parameters)
    {
        return Tool.CreateFunctionTool(name, description, parameters);
    }
}