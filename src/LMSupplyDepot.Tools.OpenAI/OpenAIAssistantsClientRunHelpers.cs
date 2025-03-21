namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Extension methods for OpenAIAssistantsClient to handle runs with required actions
/// </summary>
public partial class OpenAIAssistantsClient
{
    /// <summary>
    /// Creates a run and waits for completion or required action
    /// </summary>
    public async Task<Run> CreateAndWaitForRunAsync(
        string threadId,
        string assistantId,
        int pollIntervalMs = 1000,
        int maxAttempts = 0,
        CancellationToken cancellationToken = default)
    {
        var run = await CreateSimpleRunAsync(threadId, assistantId, cancellationToken);
        return await WaitForRunCompletionOrActionAsync(threadId, run.Id, pollIntervalMs, maxAttempts, cancellationToken);
    }

    /// <summary>
    /// Waits for a run to complete or require action
    /// </summary>
    public async Task<Run> WaitForRunCompletionOrActionAsync(
        string threadId,
        string runId,
        int pollIntervalMs = 1000,
        int maxAttempts = 0,
        CancellationToken cancellationToken = default)
    {
        var attempts = 0;
        while (maxAttempts == 0 || attempts < maxAttempts)
        {
            var run = await RetrieveRunAsync(threadId, runId, cancellationToken);
            switch (run.Status)
            {
                case RunStatus.Completed:
                case RunStatus.Failed:
                case RunStatus.Cancelled:
                case RunStatus.Expired:
                case RunStatus.Incomplete:
                case RunStatus.RequiresAction:
                    return run;
                default:
                    attempts++;
                    await Task.Delay(pollIntervalMs, cancellationToken);
                    break;
            }
        }

        throw new TimeoutException($"Run did not complete or require action after {maxAttempts} polling attempts.");
    }

    /// <summary>
    /// Handles function tool calls for a run automatically
    /// </summary>
    public async Task<Run> HandleFunctionToolCallsAsync(
        string threadId,
        Run run,
        Dictionary<string, Func<string, Task<string>>> functionHandlers,
        bool continuePolling = true,
        int pollIntervalMs = 1000,
        int maxAttempts = 0,
        CancellationToken cancellationToken = default)
    {
        if (run == null || !run.RequiresToolOutputs())
            return run;

        var toolCalls = run.GetRequiredToolCalls();
        var toolOutputs = new List<ToolOutput>();

        foreach (var toolCall in toolCalls)
        {
            if (toolCall.Type == ToolTypes.Function)
            {
                var function = toolCall.Function;
                if (function != null && functionHandlers.TryGetValue(function.Name, out var handler))
                {
                    try
                    {
                        var output = await handler(function.Arguments);
                        toolOutputs.Add(ToolOutput.Create(toolCall.Id, output));
                    }
                    catch (Exception ex)
                    {
                        // If function handler throws, return the error as output
                        toolOutputs.Add(ToolOutput.Create(toolCall.Id,
                            JsonSerializer.Serialize(new { error = ex.Message })));
                    }
                }
                else
                {
                    // If no handler found, return an error
                    toolOutputs.Add(ToolOutput.Create(toolCall.Id,
                        JsonSerializer.Serialize(new { error = $"No handler found for function {function?.Name}" })));
                }
            }
        }

        // Submit the tool outputs
        var request = SubmitToolOutputsRequest.Create(toolOutputs);
        var updatedRun = await SubmitToolOutputsAsync(threadId, run.Id, request, cancellationToken);

        // Continue polling if needed
        if (continuePolling)
        {
            return await WaitForRunCompletionOrActionAsync(threadId, updatedRun.Id, pollIntervalMs, maxAttempts, cancellationToken);
        }

        return updatedRun;
    }

    /// <summary>
    /// Creates a thread, adds a message, starts a run, and handles function tool calls
    /// </summary>
    public async Task<(ChatThread Thread, Message Message, Run Run)> CreateThreadRunAndHandleFunctionsAsync(
        string assistantId,
        string userMessage,
        Dictionary<string, Func<string, Task<string>>> functionHandlers,
        int pollIntervalMs = 1000,
        int maxAttempts = 0,
        CancellationToken cancellationToken = default)
    {
        // Create thread and run
        var (thread, message, run) = await CreateThreadAndRunAsync(assistantId, userMessage, cancellationToken);

        // Wait for completion or action
        run = await WaitForRunCompletionOrActionAsync(thread.Id, run.Id, pollIntervalMs, maxAttempts, cancellationToken);

        // Handle function calls if needed
        if (run.RequiresToolOutputs())
        {
            run = await HandleFunctionToolCallsAsync(
                thread.Id, run, functionHandlers, true, pollIntervalMs, maxAttempts, cancellationToken);
        }

        return (thread, message, run);
    }

    /// <summary>
    /// Gets all messages created during a run
    /// </summary>
    public async Task<List<Message>> GetMessagesFromRunAsync(
        string threadId,
        string runId,
        CancellationToken cancellationToken = default)
    {
        var steps = await ListRunStepsAsync(threadId, runId, order: "asc", cancellationToken: cancellationToken);
        var messageIds = new List<string>();

        foreach (var step in steps.Data)
        {
            var messageId = step.GetCreatedMessageId();
            if (!string.IsNullOrEmpty(messageId))
            {
                messageIds.Add(messageId);
            }
        }

        var messages = new List<Message>();
        foreach (var messageId in messageIds)
        {
            var message = await RetrieveMessageAsync(threadId, messageId, cancellationToken);
            messages.Add(message);
        }

        return messages;
    }
}