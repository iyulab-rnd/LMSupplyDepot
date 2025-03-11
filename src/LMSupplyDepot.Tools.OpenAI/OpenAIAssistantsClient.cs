using ChatThread = LMSupplyDepot.Tools.OpenAI.Models.ChatThread;

namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI Assistants API
/// </summary>
public partial class OpenAIAssistantsClient : OpenAIBaseClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIAssistantsClient"/> class
    /// </summary>
    public OpenAIAssistantsClient(string apiKey, HttpClient httpClient = null)
        : base(apiKey, httpClient)
    {
        SetupAssistantsApiHeader();
    }

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
    public async Task<ListResponse<Assistant>> ListAssistantsAsync(int? limit = null, string order = null, string after = null, string before = null, CancellationToken cancellationToken = default)
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

    #region Files

    /// <summary>
    /// Uploads a file to OpenAI
    /// </summary>
    public async Task<dynamic> UploadFileAsync(string filePath, string purpose, CancellationToken cancellationToken = default)
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(purpose), "purpose");

        using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var fileContent = new StreamContent(fileStream);
        var fileName = Path.GetFileName(filePath);
        content.Add(fileContent, "file", fileName);

        var url = $"{BaseUrl}/{ApiVersion}/files";
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        await EnsureSuccessStatusCodeAsync(response);

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<dynamic>(responseContent, _jsonOptions);
    }

    /// <summary>
    /// Retrieves a file
    /// </summary>
    public async Task<dynamic> RetrieveFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        return await SendRequestAsync<dynamic>(HttpMethod.Get, $"/{ApiVersion}/files/{fileId}", null, cancellationToken);
    }

    /// <summary>
    /// Retrieves the content of a file
    /// </summary>
    public async Task<Stream> RetrieveFileContentAsync(string fileId, CancellationToken cancellationToken = default)
    {
        var url = $"{BaseUrl}/{ApiVersion}/files/{fileId}/content";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessStatusCodeAsync(response);

        return await response.Content.ReadAsStreamAsync();
    }

    /// <summary>
    /// Lists files
    /// </summary>
    public async Task<dynamic> ListFilesAsync(string purpose = null, CancellationToken cancellationToken = default)
    {
        var parameters = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(purpose)) parameters["purpose"] = purpose;

        var queryString = BuildQueryString(parameters);
        return await SendRequestAsync<dynamic>(HttpMethod.Get, $"/{ApiVersion}/files{queryString}", null, cancellationToken);
    }

    /// <summary>
    /// Deletes a file
    /// </summary>
    public async Task<bool> DeleteFileAsync(string fileId, CancellationToken cancellationToken = default)
    {
        await SendRequestAsync<dynamic>(HttpMethod.Delete, $"/{ApiVersion}/files/{fileId}", null, cancellationToken);
        return true;
    }

    #endregion

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