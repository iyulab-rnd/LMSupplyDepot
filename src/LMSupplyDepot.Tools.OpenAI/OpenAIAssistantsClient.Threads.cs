using ChatThread = LMSupplyDepot.Tools.OpenAI.Models.ChatThread;

namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI Assistants API - Threads functionality
/// </summary>
public partial class OpenAIAssistantsClient
{
    #region Threads

    /// <summary>
    /// Lists threads
    /// </summary>
    public async Task<ListResponse<ChatThread>> ListThreadsAsync(int? limit = null, string order = null, string after = null, string before = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>();
        if (limit.HasValue) parameters["limit"] = limit.Value.ToString();
        if (!string.IsNullOrEmpty(order)) parameters["order"] = order;
        if (!string.IsNullOrEmpty(after)) parameters["after"] = after;
        if (!string.IsNullOrEmpty(before)) parameters["before"] = before;

        var queryString = BuildQueryString(parameters);
        return await SendRequestAsync<ListResponse<ChatThread>>(HttpMethod.Get, $"/{ApiVersion}/threads{queryString}", null, cancellationToken);
    }

    /// <summary>
    /// Creates a new thread
    /// </summary>
    public async Task<ChatThread> CreateThreadAsync(CreateThreadRequest request = null, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<ChatThread>(HttpMethod.Post, $"/{ApiVersion}/threads", request ?? CreateThreadRequest.Create(), cancellationToken);
    }

    /// <summary>
    /// Retrieves a thread
    /// </summary>
    public async Task<ChatThread> RetrieveThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<ChatThread>(HttpMethod.Get, $"/{ApiVersion}/threads/{threadId}", null, cancellationToken);
    }

    /// <summary>
    /// Updates a thread
    /// </summary>
    public async Task<ChatThread> UpdateThreadAsync(string threadId, UpdateThreadRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<ChatThread>(HttpMethod.Post, $"/{ApiVersion}/threads/{threadId}", request, cancellationToken);
    }

    /// <summary>
    /// Deletes a thread
    /// </summary>
    public async Task<bool> DeleteThreadAsync(string threadId, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync<dynamic>(HttpMethod.Delete, $"/{ApiVersion}/threads/{threadId}", null, cancellationToken);
        return true;
    }

    #endregion
}