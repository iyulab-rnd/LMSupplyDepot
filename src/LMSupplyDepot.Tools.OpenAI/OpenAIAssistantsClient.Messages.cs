namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI Assistants API - Messages functionality
/// </summary>
public partial class OpenAIAssistantsClient
{
    #region Messages

    /// <summary>
    /// Creates a new message in a thread
    /// </summary>
    public async Task<Message> CreateMessageAsync(string threadId, CreateMessageRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Message>(HttpMethod.Post, $"/{ApiVersion}/threads/{threadId}/messages", request, cancellationToken);
    }

    /// <summary>
    /// Creates a user message in a thread with simple content
    /// </summary>
    public async Task<Message> CreateUserMessageAsync(string threadId, string content, CancellationToken cancellationToken = default)
    {
        var request = CreateMessageRequest.Create(content);
        return await CreateMessageAsync(threadId, request, cancellationToken);
    }

    /// <summary>
    /// Retrieves a message from a thread
    /// </summary>
    public async Task<Message> RetrieveMessageAsync(string threadId, string messageId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Message>(HttpMethod.Get, $"/{ApiVersion}/threads/{threadId}/messages/{messageId}", null, cancellationToken);
    }

    /// <summary>
    /// Lists messages in a thread
    /// </summary>
    public async Task<ListResponse<Message>> ListMessagesAsync(string threadId, int? limit = null, string order = null, string after = null, string before = null, string runId = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>();
        if (limit.HasValue) parameters["limit"] = limit.Value.ToString();
        if (!string.IsNullOrEmpty(order)) parameters["order"] = order;
        if (!string.IsNullOrEmpty(after)) parameters["after"] = after;
        if (!string.IsNullOrEmpty(before)) parameters["before"] = before;
        if (!string.IsNullOrEmpty(runId)) parameters["run_id"] = runId;

        var queryString = BuildQueryString(parameters);
        return await SendRequestAsync<ListResponse<Message>>(HttpMethod.Get, $"/{ApiVersion}/threads/{threadId}/messages{queryString}", null, cancellationToken);
    }

    #endregion
}