namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI Assistants API - Run Steps functionality
/// </summary>
public partial class OpenAIAssistantsClient
{
    #region Run Steps

    /// <summary>
    /// Retrieves a run step from a run
    /// </summary>
    public async Task<RunStep> RetrieveRunStepAsync(string threadId, string runId, string stepId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<RunStep>(HttpMethod.Get, $"/{ApiVersion}/threads/{threadId}/runs/{runId}/steps/{stepId}", null, cancellationToken);
    }

    /// <summary>
    /// Lists steps from a run
    /// </summary>
    public async Task<ListResponse<RunStep>> ListRunStepsAsync(string threadId, string runId, int? limit = null, string order = null, string after = null, string before = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>();
        if (limit.HasValue) parameters["limit"] = limit.Value.ToString();
        if (!string.IsNullOrEmpty(order)) parameters["order"] = order;
        if (!string.IsNullOrEmpty(after)) parameters["after"] = after;
        if (!string.IsNullOrEmpty(before)) parameters["before"] = before;

        var queryString = BuildQueryString(parameters);
        return await SendRequestAsync<ListResponse<RunStep>>(HttpMethod.Get, $"/{ApiVersion}/threads/{threadId}/runs/{runId}/steps{queryString}", null, cancellationToken);
    }

    #endregion
}