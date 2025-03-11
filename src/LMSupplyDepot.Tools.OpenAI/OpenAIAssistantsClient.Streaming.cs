namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Extension methods for OpenAIAssistantsClient to support streaming
/// </summary>
public partial class OpenAIAssistantsClient
{
    /// <summary>
    /// Creates a run handler for streaming using Server-Sent Events (SSE)
    /// </summary>
    public RunStreamHandler PrepareStreamingRun(
        string threadId,
        string assistantId,
        CancellationToken cancellationToken = default)
    {
        // Create a new stream handler with the API key
        return new RunStreamHandler(this, this.GetApKey(), threadId, assistantId);
    }

    /// <summary>
    /// Creates a thread, adds a message, and prepares a streaming run in one operation
    /// </summary>
    public async System.Threading.Tasks.Task<(string ThreadId, RunStreamHandler StreamHandler)> CreateThreadWithMessageAndPrepareStreamingRunAsync(
        string assistantId,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        // Create a thread with the initial message
        var createThreadRequest = CreateThreadRequest.Create()
            .WithMessages(new System.Collections.Generic.List<CreateMessageRequest> { CreateMessageRequest.Create(userMessage) });

        var thread = await CreateThreadAsync(createThreadRequest, cancellationToken);

        // Create a streaming handler for the thread
        var streamHandler = PrepareStreamingRun(thread.Id, assistantId, cancellationToken);

        return (thread.Id, streamHandler);
    }
}