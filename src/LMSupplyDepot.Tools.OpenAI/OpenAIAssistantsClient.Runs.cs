namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI Assistants API - Runs functionality
/// </summary>
public partial class OpenAIAssistantsClient
{
    #region Runs

    /// <summary>
    /// Creates a new run for a thread
    /// </summary>
    public async Task<Run> CreateRunAsync(string threadId, CreateRunRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Run>(HttpMethod.Post, $"/{ApiVersion}/threads/{threadId}/runs", request, cancellationToken);
    }

    /// <summary>
    /// Creates a simple run for a thread with an assistant
    /// </summary>
    public async Task<Run> CreateSimpleRunAsync(string threadId, string assistantId, CancellationToken cancellationToken = default)
    {
        var request = CreateRunRequest.Create(assistantId);
        return await CreateRunAsync(threadId, request, cancellationToken);
    }

    /// <summary>
    /// Retrieves a run from a thread
    /// </summary>
    public async Task<Run> RetrieveRunAsync(string threadId, string runId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Run>(HttpMethod.Get, $"/{ApiVersion}/threads/{threadId}/runs/{runId}", null, cancellationToken);
    }

    /// <summary>
    /// Lists runs in a thread
    /// </summary>
    public async Task<ListResponse<Run>> ListRunsAsync(string threadId, int? limit = null, string order = null, string after = null, string before = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>();
        if (limit.HasValue) parameters["limit"] = limit.Value.ToString();
        if (!string.IsNullOrEmpty(order)) parameters["order"] = order;
        if (!string.IsNullOrEmpty(after)) parameters["after"] = after;
        if (!string.IsNullOrEmpty(before)) parameters["before"] = before;

        var queryString = BuildQueryString(parameters);
        return await SendRequestAsync<ListResponse<Run>>(HttpMethod.Get, $"/{ApiVersion}/threads/{threadId}/runs{queryString}", null, cancellationToken);
    }

    /// <summary>
    /// Cancels a run
    /// </summary>
    public async Task<Run> CancelRunAsync(string threadId, string runId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Run>(HttpMethod.Post, $"/{ApiVersion}/threads/{threadId}/runs/{runId}/cancel", null, cancellationToken);
    }

    /// <summary>
    /// Submits tool outputs to a run that requires action
    /// </summary>
    public async Task<Run> SubmitToolOutputsAsync(string threadId, string runId, SubmitToolOutputsRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Run>(HttpMethod.Post, $"/{ApiVersion}/threads/{threadId}/runs/{runId}/submit_tool_outputs", request, cancellationToken);
    }

    /// <summary>
    /// Waits for a run to complete, polling at the specified interval
    /// </summary>
    public async Task<Run> WaitForRunCompletionAsync(string threadId, string runId, int pollIntervalMs = 1000, int maxAttempts = 0, CancellationToken cancellationToken = default)
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
                    return run;
                case RunStatus.RequiresAction:
                    throw new OpenAIException("Run requires action. Use SubmitToolOutputsAsync to provide tool outputs.", System.Net.HttpStatusCode.BadRequest, "run_requires_action");
                default:
                    attempts++;
                    await Task.Delay(pollIntervalMs, cancellationToken);
                    break;
            }
        }

        throw new TimeoutException($"Run did not complete after {maxAttempts} polling attempts.");
    }

    #endregion
}