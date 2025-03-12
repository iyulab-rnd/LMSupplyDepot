namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Extension methods for thread and message operations
/// </summary>
public static class ThreadMessageExtensions
{
    /// <summary>
    /// Sets the tool resources for a thread using strongly typed ToolResources
    /// </summary>
    public static CreateThreadRequest WithToolResources(this CreateThreadRequest request, ToolResources toolResources)
    {
        request.SetValue(PropertyNames.ToolResources, toolResources);
        return request;
    }

    /// <summary>
    /// Sets the file search tool resources for a thread
    /// </summary>
    public static CreateThreadRequest WithFileSearchResources(this CreateThreadRequest request, List<string> vectorStoreIds)
    {
        if (vectorStoreIds == null || vectorStoreIds.Count == 0)
            return request;

        var toolResources = ToolResources.CreateForFileSearch(vectorStoreIds);
        return request.WithToolResources(toolResources);
    }

    /// <summary>
    /// Sets the code interpreter tool resources for a thread
    /// </summary>
    public static CreateThreadRequest WithCodeInterpreterResources(this CreateThreadRequest request, List<string> fileIds)
    {
        if (fileIds == null || fileIds.Count == 0)
            return request;

        var toolResources = ToolResources.CreateForCodeInterpreter(fileIds);
        return request.WithToolResources(toolResources);
    }

    /// <summary>
    /// Sets both file search and code interpreter tool resources for a thread
    /// </summary>
    public static CreateThreadRequest WithCombinedToolResources(
        this CreateThreadRequest request,
        List<string> vectorStoreIds,
        List<string> codeInterpreterFileIds)
    {
        if ((vectorStoreIds == null || vectorStoreIds.Count == 0) &&
            (codeInterpreterFileIds == null || codeInterpreterFileIds.Count == 0))
            return request;

        var toolResources = ToolResources.CreateCombined(vectorStoreIds, codeInterpreterFileIds);
        return request.WithToolResources(toolResources);
    }

    /// <summary>
    /// Similar extension methods for UpdateThreadRequest
    /// </summary>
    public static UpdateThreadRequest WithToolResources(this UpdateThreadRequest request, ToolResources toolResources)
    {
        request.SetValue(PropertyNames.ToolResources, toolResources);
        return request;
    }

    /// <summary>
    /// Sets the file search tool resources for a thread update
    /// </summary>
    public static UpdateThreadRequest WithFileSearchResources(this UpdateThreadRequest request, List<string> vectorStoreIds)
    {
        if (vectorStoreIds == null || vectorStoreIds.Count == 0)
            return request;

        var toolResources = ToolResources.CreateForFileSearch(vectorStoreIds);
        return request.WithToolResources(toolResources);
    }

    /// <summary>
    /// Sets the code interpreter tool resources for a thread update
    /// </summary>
    public static UpdateThreadRequest WithCodeInterpreterResources(this UpdateThreadRequest request, List<string> fileIds)
    {
        if (fileIds == null || fileIds.Count == 0)
            return request;

        var toolResources = ToolResources.CreateForCodeInterpreter(fileIds);
        return request.WithToolResources(toolResources);
    }

    /// <summary>
    /// Sets both file search and code interpreter tool resources for a thread update
    /// </summary>
    public static UpdateThreadRequest WithCombinedToolResources(
        this UpdateThreadRequest request,
        List<string> vectorStoreIds,
        List<string> codeInterpreterFileIds)
    {
        if ((vectorStoreIds == null || vectorStoreIds.Count == 0) &&
            (codeInterpreterFileIds == null || codeInterpreterFileIds.Count == 0))
            return request;

        var toolResources = ToolResources.CreateCombined(vectorStoreIds, codeInterpreterFileIds);
        return request.WithToolResources(toolResources);
    }
}