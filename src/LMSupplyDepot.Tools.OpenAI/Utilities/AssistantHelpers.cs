namespace LMSupplyDepot.Tools.OpenAI.Utilities;

/// <summary>
/// Helper methods for working with the Assistants API
/// </summary>
public static class AssistantHelpers
{
    /// <summary>
    /// Creates a simple text dictionary for tool resources
    /// </summary>
    public static Dictionary<string, object> CreateToolResources(string type, List<string> fileIds)
    {
        var toolResources = new Dictionary<string, object>();

        if (fileIds != null && fileIds.Count > 0)
        {
            toolResources[type] = new Dictionary<string, object>
            {
                { "file_ids", fileIds }
            };
        }

        return toolResources;
    }

    /// <summary>
    /// Creates code interpreter tool resources
    /// </summary>
    public static Dictionary<string, object> CreateCodeInterpreterToolResources(List<string> fileIds)
    {
        return CreateToolResources(ToolTypes.CodeInterpreter, fileIds);
    }

    /// <summary>
    /// Creates file search tool resources
    /// </summary>
    public static Dictionary<string, object> CreateFileSearchToolResources(List<string> vectorStoreIds)
    {
        var toolResources = new Dictionary<string, object>();

        if (vectorStoreIds != null && vectorStoreIds.Count > 0)
        {
            toolResources[ToolTypes.FileSearch] = new Dictionary<string, object>
            {
                { "vector_store_ids", vectorStoreIds }
            };
        }

        return toolResources;
    }

    /// <summary>
    /// Creates combined tool resources for multiple tool types
    /// </summary>
    public static Dictionary<string, object> CreateCombinedToolResources(List<string> fileSearchVectorStoreIds = null, List<string> codeInterpreterFileIds = null)
    {
        var toolResources = new Dictionary<string, object>();

        if (fileSearchVectorStoreIds != null && fileSearchVectorStoreIds.Count > 0)
        {
            toolResources[ToolTypes.FileSearch] = new Dictionary<string, object>
            {
                { "vector_store_ids", fileSearchVectorStoreIds }
            };
        }

        if (codeInterpreterFileIds != null && codeInterpreterFileIds.Count > 0)
        {
            toolResources[ToolTypes.CodeInterpreter] = new Dictionary<string, object>
            {
                { "file_ids", codeInterpreterFileIds }
            };
        }

        return toolResources;
    }

    /// <summary>
    /// Extracts plain text content from a message
    /// </summary>
    public static string GetMessageText(Message message)
    {
        if (message?.Content == null || message.Content.Count == 0)
        {
            return string.Empty;
        }

        var textContentList = message.Content
            .Where(c => c.Type == "text")
            .Select(c => c.GetTextContent()?.Value)
            .Where(text => !string.IsNullOrEmpty(text))
            .ToList();

        return string.Join("\n", textContentList);
    }

    /// <summary>
    /// Extracts file objects from a message
    /// </summary>
    public static List<object> GetMessageFiles(Message message)
    {
        if (message?.Content == null || message.Content.Count == 0)
        {
            return new List<object>();
        }

        return message.Content
            .Where(c => c.Type == "image_file")
            .Select(c => c.GetImageFileContent())
            .Where(file => file != null)
            .Cast<object>()
            .ToList();
    }

    /// <summary>
    /// Creates file citation text from annotations in a message
    /// </summary>
    public static async Task<string> GetMessageWithCitationsAsync(Message message, OpenAIAssistantsClient client)
    {
        if (message?.Content == null || message.Content.Count == 0)
        {
            return string.Empty;
        }

        var textContent = message.Content.FirstOrDefault(c => c.Type == "text")?.GetTextContent();
        if (textContent == null)
        {
            return string.Empty;
        }

        var content = textContent.Value;
        var annotations = textContent.GetAnnotations();

        if (annotations == null || annotations.Count == 0)
        {
            return content;
        }

        var citations = new List<string>();
        for (int i = 0; i < annotations.Count; i++)
        {
            var annotation = annotations[i];
            content = content.Replace(annotation.Text, $"[{i + 1}]");

            if (annotation.Type == "file_citation")
            {
                // Get file_citation using the flexible approach
                var fileCitationObj = annotation.GetValue<Dictionary<string, JsonElement>>("file_citation");
                string? fileId = null;
                string? quote = null;

                if (fileCitationObj != null)
                {
                    if (fileCitationObj.TryGetValue("file_id", out var fileIdElement))
                    {
                        fileId = fileIdElement.GetString();
                    }
                    if (fileCitationObj.TryGetValue("quote", out var quoteElement))
                    {
                        quote = quoteElement.GetString();
                    }
                }

                if (!string.IsNullOrEmpty(fileId))
                {
                    var file = await client.RetrieveFileAsync(fileId);
                    string filename = JsonSerializer.Deserialize<JsonElement>(file.ToString()).GetProperty("filename").GetString();
                    citations.Add($"[{i + 1}] {quote} (Source: {filename})");
                }
            }
            else if (annotation.Type == "file_path")
            {
                // Get file_path using the flexible approach
                var filePathObj = annotation.GetValue<Dictionary<string, JsonElement>>("file_path");
                string? fileId = null;

                if (filePathObj != null && filePathObj.TryGetValue("file_id", out var fileIdElement))
                {
                    fileId = fileIdElement.GetString();
                }

                if (!string.IsNullOrEmpty(fileId))
                {
                    var file = await client.RetrieveFileAsync(fileId);
                    string filename = JsonSerializer.Deserialize<JsonElement>(file.ToString()).GetProperty("filename").GetString();
                    citations.Add($"[{i + 1}] File: {filename} (ID: {fileId})");
                }
            }
        }

        if (citations.Count > 0)
        {
            content += "\n\nReferences:\n" + string.Join("\n", citations);
        }

        return content;
    }

    /// <summary>
    /// Runs a conversation from start to finish with complete conversation history
    /// </summary>
    public static async Task<List<Message>> RunConversationAsync(
        OpenAIAssistantsClient client,
        string assistantId,
        List<string> messages,
        int pollIntervalMs = 1000,
        int maxAttempts = 0,
        CancellationToken cancellationToken = default)
    {
        // Create the thread
        var thread = await client.CreateThreadAsync(cancellationToken: cancellationToken);

        // Add each message to the thread
        foreach (var message in messages)
        {
            await client.CreateUserMessageAsync(thread.Id, message, cancellationToken);
        }

        // Run the assistant
        var run = await client.CreateSimpleRunAsync(thread.Id, assistantId, cancellationToken);

        // Wait for the run to complete
        await client.WaitForRunCompletionAsync(thread.Id, run.Id, pollIntervalMs, maxAttempts, cancellationToken);

        // Get all messages in the thread
        var messagesResponse = await client.ListMessagesAsync(thread.Id, order: "asc", cancellationToken: cancellationToken);
        return messagesResponse.Data;
    }

    /// <summary>
    /// Creates an expiration policy object
    /// </summary>
    public static Dictionary<string, object> CreateExpirationPolicy(string anchor, int days)
    {
        return new Dictionary<string, object>
        {
            { "anchor", anchor },
            { "days", days }
        };
    }

    /// <summary>
    /// Creates a last active expiration policy object
    /// </summary>
    public static Dictionary<string, object> CreateLastActiveExpirationPolicy(int days)
    {
        return CreateExpirationPolicy("last_active_at", days);
    }
}