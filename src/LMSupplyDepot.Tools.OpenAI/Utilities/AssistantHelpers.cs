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
    /// Creates file search tool resources with vector store IDs
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
    public static Dictionary<string, object> CreateCombinedToolResources(
        List<string> fileSearchVectorStoreIds = null,
        List<string> fileSearchFileIds = null,
        List<string> codeInterpreterFileIds = null)
    {
        var toolResources = new Dictionary<string, object>();

        if (fileSearchVectorStoreIds != null && fileSearchVectorStoreIds.Count > 0)
        {
            toolResources[ToolTypes.FileSearch] = new Dictionary<string, object>
            {
                { "vector_store_ids", fileSearchVectorStoreIds }
            };
        }
        else if (fileSearchFileIds != null && fileSearchFileIds.Count > 0)
        {
            toolResources[ToolTypes.FileSearch] = new Dictionary<string, object>
            {
                { "file_ids", fileSearchFileIds }
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
            .FindAll(c => c.Type == "text")
            .ConvertAll(c => c.Text?.Value)
            .FindAll(text => !string.IsNullOrEmpty(text));

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
            .FindAll(c => c.Type == "image_file")
            .ConvertAll(c => c.ImageFile!)
            .FindAll(file => file != null)
            .ConvertAll(f => (object)f);
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