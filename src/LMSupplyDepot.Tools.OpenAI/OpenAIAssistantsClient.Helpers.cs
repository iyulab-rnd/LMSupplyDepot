using ChatThread = LMSupplyDepot.Tools.OpenAI.Models.ChatThread;

namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI Assistants API - Helper Methods
/// </summary>
public partial class OpenAIAssistantsClient
{
    #region Convenient Helper Methods

    /// <summary>
    /// Creates a thread, adds a message, and starts a run in one operation
    /// </summary>
    public async Task<(ChatThread Thread, Message Message, Run Run)> CreateThreadAndRunAsync(string assistantId, string userMessage, CancellationToken cancellationToken = default)
    {
        // Create the thread with the initial message
        var createThreadRequest = CreateThreadRequest.Create()
            .WithMessages(new List<CreateMessageRequest> { CreateMessageRequest.Create(userMessage) });

        var thread = await CreateThreadAsync(createThreadRequest, cancellationToken);

        // Get the first message
        var messagesResponse = await ListMessagesAsync(thread.Id, cancellationToken: cancellationToken);
        var message = messagesResponse.Data[0];

        // Create a run
        var run = await CreateSimpleRunAsync(thread.Id, assistantId, cancellationToken);

        return (thread, message, run);
    }

    /// <summary>
    /// Waits for a run to complete and then returns the assistant's response
    /// </summary>
    public async Task<Message> GetAssistantResponseAsync(string threadId, string runId, int pollIntervalMs = 1000, int maxAttempts = 0, CancellationToken cancellationToken = default)
    {
        var run = await WaitForRunCompletionAsync(threadId, runId, pollIntervalMs, maxAttempts, cancellationToken);

        if (run.Status != RunStatus.Completed)
        {
            throw new OpenAIException($"Run did not complete successfully. Status: {run.Status}", System.Net.HttpStatusCode.BadRequest, "run_incomplete");
        }

        var messagesResponse = await ListMessagesAsync(threadId, order: "desc", limit: 1, cancellationToken: cancellationToken);
        return messagesResponse.Data[0];
    }

    /// <summary>
    /// Creates a complete conversation with an assistant in one operation
    /// </summary>
    public async Task<Message> GetSimpleResponseAsync(string assistantId, string userMessage, int pollIntervalMs = 1000, int maxAttempts = 0, CancellationToken cancellationToken = default)
    {
        var (thread, _, run) = await CreateThreadAndRunAsync(assistantId, userMessage, cancellationToken);
        return await GetAssistantResponseAsync(thread.Id, run.Id, pollIntervalMs, maxAttempts, cancellationToken);
    }

    #endregion
}