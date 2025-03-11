namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI Assistants API - Streaming functionality
/// </summary>
public partial class OpenAIAssistantsClient
{
    #region Streaming

    /// <summary>
    /// Creates a run handler for streaming using Server-Sent Events (SSE)
    /// </summary>
    public RunStreamHandler PrepareStreamingRun(
        string threadId,
        string assistantId,
        CancellationToken cancellationToken = default)
    {
        // Create a new stream handler with the API key
        return new RunStreamHandler(this, this.GetApiKey(), threadId, assistantId);
    }

    /// <summary>
    /// Creates a run handler for streaming with specified tools and tool resources
    /// </summary>
    public RunStreamHandler PrepareStreamingRunWithTools(
        string threadId,
        string assistantId,
        List<Tool> tools = null,
        Dictionary<string, object> toolResources = null,
        CancellationToken cancellationToken = default)
    {
        // Create a new stream handler with configuration
        return new RunStreamHandler(
            this,
            this.GetApiKey(),
            threadId,
            assistantId,
            tools,
            toolResources);
    }

    #endregion
}