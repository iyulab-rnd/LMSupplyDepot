namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI Assistants API - Assistants functionality
/// </summary>
public partial class OpenAIAssistantsClient
{
    #region Assistants

    /// <summary>
    /// Creates a new assistant
    /// </summary>
    public async Task<Assistant> CreateAssistantAsync(CreateAssistantRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Assistant>(HttpMethod.Post, $"/{ApiVersion}/assistants", request, cancellationToken);
    }

    /// <summary>
    /// Retrieves an assistant
    /// </summary>
    public async Task<Assistant> RetrieveAssistantAsync(string assistantId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Assistant>(HttpMethod.Get, $"/{ApiVersion}/assistants/{assistantId}", null, cancellationToken);
    }

    /// <summary>
    /// Updates an assistant
    /// </summary>
    public async Task<Assistant> UpdateAssistantAsync(string assistantId, UpdateAssistantRequest request, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<Assistant>(HttpMethod.Post, $"/{ApiVersion}/assistants/{assistantId}", request, cancellationToken);
    }

    /// <summary>
    /// Deletes an assistant
    /// </summary>
    public async Task<bool> DeleteAssistantAsync(string assistantId, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync<dynamic>(HttpMethod.Delete, $"/{ApiVersion}/assistants/{assistantId}", null, cancellationToken);
        return true;
    }

    /// <summary>
    /// Lists assistants
    /// </summary>
    public async Task<ListResponse<Assistant>> ListAssistantsAsync(int? limit = null, string? order = null, string? after = null, string? before = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>();
        if (limit.HasValue) parameters["limit"] = limit.Value.ToString();
        if (!string.IsNullOrEmpty(order)) parameters["order"] = order;
        if (!string.IsNullOrEmpty(after)) parameters["after"] = after;
        if (!string.IsNullOrEmpty(before)) parameters["before"] = before;

        var queryString = BuildQueryString(parameters);
        return await SendRequestAsync<ListResponse<Assistant>>(HttpMethod.Get, $"/{ApiVersion}/assistants{queryString}", null, cancellationToken);
    }

    #endregion
}