namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Extension methods for CreateRunRequest
/// </summary>
public static class RunRequestExtensions
{
    /// <summary>
    /// Sets the truncation strategy for this run
    /// </summary>
    public static CreateRunRequest WithTruncationStrategy(this CreateRunRequest request, TruncationStrategy truncationStrategy)
    {
        request.SetValue(PropertyNames.TruncationStrategy, truncationStrategy);
        return request;
    }

    /// <summary>
    /// Sets the auto truncation strategy for this run
    /// </summary>
    public static CreateRunRequest WithAutoTruncation(this CreateRunRequest request)
    {
        return request.WithTruncationStrategy(TruncationStrategy.CreateAuto());
    }

    /// <summary>
    /// Sets the first messages truncation strategy for this run
    /// </summary>
    public static CreateRunRequest WithFirstMessagesTruncation(this CreateRunRequest request)
    {
        return request.WithTruncationStrategy(TruncationStrategy.CreateFirst());
    }

    /// <summary>
    /// Sets the last messages truncation strategy for this run
    /// </summary>
    public static CreateRunRequest WithLastMessagesTruncation(this CreateRunRequest request)
    {
        return request.WithTruncationStrategy(TruncationStrategy.CreateLast());
    }

    /// <summary>
    /// Sets the first and last messages truncation strategy for this run
    /// </summary>
    public static CreateRunRequest WithFirstAndLastMessagesTruncation(this CreateRunRequest request, int firstMessages, int lastMessages)
    {
        return request.WithTruncationStrategy(TruncationStrategy.CreateFirstAndLast(firstMessages, lastMessages));
    }

    /// <summary>
    /// Sets the tool resources for this run with strongly typed ToolResources
    /// </summary>
    public static CreateRunRequest WithToolResources(this CreateRunRequest request, ToolResources toolResources)
    {
        request.SetValue(PropertyNames.ToolResources, toolResources);
        return request;
    }

    /// <summary>
    /// Configures this run for file search with the specified vector store IDs using strongly typed ToolResources
    /// </summary>
    public static CreateRunRequest ConfigureForFileSearch(
        this CreateRunRequest request,
        List<string> vectorStoreIds,
        int? maxNumResults = null,
        string? ranker = null,
        double? scoreThreshold = null)
    {
        // Add file search tool
        request.WithFileSearchTool(maxNumResults, ranker, scoreThreshold);

        // Add vector store IDs as resources
        var toolResources = ToolResources.CreateForFileSearch(vectorStoreIds);
        request.WithToolResources(toolResources);

        return request;
    }

    /// <summary>
    /// Configures this run for code interpreter with the specified file IDs using strongly typed ToolResources
    /// </summary>
    public static CreateRunRequest ConfigureForCodeInterpreter(
        this CreateRunRequest request,
        List<string> fileIds)
    {
        // Add code interpreter tool
        request.WithCodeInterpreterTool();

        // Add file IDs as resources
        var toolResources = ToolResources.CreateForCodeInterpreter(fileIds);
        request.WithToolResources(toolResources);

        return request;
    }

    /// <summary>
    /// Configures this run with both file search and code interpreter tools and resources
    /// </summary>
    public static CreateRunRequest ConfigureForFileSearchAndCodeInterpreter(
        this CreateRunRequest request,
        List<string> vectorStoreIds,
        List<string> codeInterpreterFileIds,
        int? maxNumResults = null,
        string? ranker = null,
        double? scoreThreshold = null)
    {
        // Add both tools
        request.WithFileSearchTool(maxNumResults, ranker, scoreThreshold);
        request.WithCodeInterpreterTool();

        // Add combined resources
        var toolResources = ToolResources.CreateCombined(vectorStoreIds, codeInterpreterFileIds);
        request.WithToolResources(toolResources);

        return request;
    }
}