namespace LMSupplyDepot.Tools.OpenAI.Utilities;

/// <summary>
/// Helper methods for working with the Assistants API
/// </summary>
public static class AssistantHelpers
{
    /// <summary>
    /// Creates tool resources for code interpreter
    /// </summary>
    public static ToolResources CreateCodeInterpreterToolResources(List<string> fileIds)
    {
        return ToolResources.CreateForCodeInterpreter(fileIds);
    }

    /// <summary>
    /// Creates file search tool resources with vector store IDs
    /// </summary>
    public static ToolResources CreateFileSearchToolResources(List<string> vectorStoreIds)
    {
        return ToolResources.CreateForFileSearch(vectorStoreIds);
    }

    /// <summary>
    /// Creates combined tool resources for multiple tool types
    /// </summary>
    public static ToolResources CreateCombinedToolResources(
        List<string> fileSearchVectorStoreIds = null,
        List<string> codeInterpreterFileIds = null)
    {
        return ToolResources.CreateCombined(fileSearchVectorStoreIds, codeInterpreterFileIds);
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
            .FindAll(c => c.Type == "text")
            .ConvertAll(c => c.Text?.Value)
            .FindAll(text => !string.IsNullOrEmpty(text));

        return string.Join("\n", textContentList);
    }

    /// <summary>
    /// Extracts file objects from a message
    /// </summary>
    public static List<ImageFileContent> GetMessageImageFiles(Message message)
    {
        if (message?.Content == null || message.Content.Count == 0)
        {
            return new List<ImageFileContent>();
        }

        return message.Content
            .FindAll(c => c.Type == "image_file")
            .ConvertAll(c => c.ImageFile)
            .Where(file => file != null)
            .ToList();
    }

    /// <summary>
    /// Creates an expiration policy object
    /// </summary>
    public static ExpirationPolicy CreateExpirationPolicy(string anchor, int days)
    {
        return new ExpirationPolicy
        {
            Anchor = anchor,
            Days = days
        };
    }

    /// <summary>
    /// Creates a last active expiration policy object
    /// </summary>
    public static ExpirationPolicy CreateLastActiveExpirationPolicy(int days)
    {
        return CreateExpirationPolicy("last_active_at", days);
    }
}