namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI Assistants API - Assistant Files functionality
/// </summary>
public partial class OpenAIAssistantsClient
{
    #region Assistant Files

    /// <summary>
    /// Creates a file attachment for an assistant
    /// </summary>
    public async Task<AssistantFile> CreateAssistantFileAsync(string assistantId, string fileId, CancellationToken cancellationToken = default)
    {
        var request = new Dictionary<string, string> { { "file_id", fileId } };
        return await SendRequestAsync<AssistantFile>(HttpMethod.Post, $"/{ApiVersion}/assistants/{assistantId}/files", request, cancellationToken);
    }

    /// <summary>
    /// Retrieves a file attachment for an assistant
    /// </summary>
    public async Task<AssistantFile> RetrieveAssistantFileAsync(string assistantId, string fileId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<AssistantFile>(HttpMethod.Get, $"/{ApiVersion}/assistants/{assistantId}/files/{fileId}", null, cancellationToken);
    }

    /// <summary>
    /// Lists all file attachments for an assistant
    /// </summary>
    public async Task<ListResponse<AssistantFile>> ListAssistantFilesAsync(string assistantId, int? limit = null, string? order = null, string? after = null, string? before = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>();
        if (limit.HasValue) parameters["limit"] = limit.Value.ToString();
        if (!string.IsNullOrEmpty(order)) parameters["order"] = order;
        if (!string.IsNullOrEmpty(after)) parameters["after"] = after;
        if (!string.IsNullOrEmpty(before)) parameters["before"] = before;

        var queryString = BuildQueryString(parameters);
        return await SendRequestAsync<ListResponse<AssistantFile>>(HttpMethod.Get, $"/{ApiVersion}/assistants/{assistantId}/files{queryString}", null, cancellationToken);
    }

    /// <summary>
    /// Deletes a file attachment from an assistant
    /// </summary>
    public async Task<DeletionResponse> DeleteAssistantFileAsync(string assistantId, string fileId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<DeletionResponse>(HttpMethod.Delete, $"/{ApiVersion}/assistants/{assistantId}/files/{fileId}", null, cancellationToken);
    }

    #endregion

    #region Message Files

    /// <summary>
    /// Retrieves a file attachment from a message
    /// </summary>
    public async Task<MessageFile> RetrieveMessageFileAsync(string threadId, string messageId, string fileId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<MessageFile>(HttpMethod.Get, $"/{ApiVersion}/threads/{threadId}/messages/{messageId}/files/{fileId}", null, cancellationToken);
    }

    /// <summary>
    /// Lists file attachments for a message
    /// </summary>
    public async Task<ListResponse<MessageFile>> ListMessageFilesAsync(string threadId, string messageId, int? limit = null, string? order = null, string? after = null, string? before = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>();
        if (limit.HasValue) parameters["limit"] = limit.Value.ToString();
        if (!string.IsNullOrEmpty(order)) parameters["order"] = order;
        if (!string.IsNullOrEmpty(after)) parameters["after"] = after;
        if (!string.IsNullOrEmpty(before)) parameters["before"] = before;

        var queryString = BuildQueryString(parameters);
        return await SendRequestAsync<ListResponse<MessageFile>>(HttpMethod.Get, $"/{ApiVersion}/threads/{threadId}/messages/{messageId}/files{queryString}", null, cancellationToken);
    }

    #endregion
}