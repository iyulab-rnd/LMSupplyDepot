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
    /// Creates a user message in a thread with file IDs
    /// </summary>
    public async Task<Message> CreateUserMessageWithFilesAsync(
        string threadId,
        string content,
        List<string> fileIds,
        CancellationToken cancellationToken = default)
    {
        var request = CreateMessageRequest.Create(content);

        if (fileIds != null && fileIds.Count > 0)
        {
            request.WithFileIds(fileIds);
        }

        return await CreateMessageAsync(threadId, request, cancellationToken);
    }

    /// <summary>
    /// Creates a user message with vector store search capability
    /// </summary>
    public async Task<Message> CreateUserMessageWithVectorStoreAsync(
        string threadId,
        string content,
        List<string> vectorStoreIds,
        CancellationToken cancellationToken = default)
    {
        if (vectorStoreIds == null || vectorStoreIds.Count == 0)
        {
            return await CreateUserMessageAsync(threadId, content, cancellationToken);
        }

        // 먼저 메시지 생성
        var request = CreateMessageRequest.Create(content);
        var message = await CreateMessageAsync(threadId, request, cancellationToken);

        // 스레드에 vector_store_ids 연결
        var toolResources = new Dictionary<string, object>
        {
            ["file_search"] = new Dictionary<string, object>
            {
                ["vector_store_ids"] = vectorStoreIds
            }
        };

        var updateRequest = UpdateThreadRequest.Create()
            .WithToolResources(toolResources);

        await UpdateThreadAsync(threadId, updateRequest, cancellationToken);

        return message;
    }

    /// <summary>
    /// Creates a user message with file search capability for specific files
    /// </summary>
    [Obsolete("This method is deprecated. Use CreateUserMessageWithVectorStoreAsync instead.")]
    public async Task<Message> CreateUserMessageWithFileSearchAsync(
        string threadId,
        string content,
        List<string> fileIds,
        CancellationToken cancellationToken = default)
    {
        if (fileIds == null || fileIds.Count == 0)
        {
            return await CreateUserMessageAsync(threadId, content, cancellationToken);
        }

        var request = CreateMessageRequest.Create(content);

        // 각 파일을 file_search 도구로 첨부
        List<MessageAttachment> attachments = new List<MessageAttachment>();
        foreach (var fileId in fileIds)
        {
            attachments.Add(MessageAttachment.Create(fileId)
                .WithFileSearchTool());
        }

        request.WithAttachments(attachments);

        return await CreateMessageAsync(threadId, request, cancellationToken);
    }

    /// <summary>
    /// Creates a user message with code interpreter capability for specific files
    /// </summary>
    public async Task<Message> CreateUserMessageWithCodeInterpreterAsync(
        string threadId,
        string content,
        List<string> fileIds,
        CancellationToken cancellationToken = default)
    {
        if (fileIds == null || fileIds.Count == 0)
        {
            return await CreateUserMessageAsync(threadId, content, cancellationToken);
        }

        var request = CreateMessageRequest.Create(content)
            .WithCodeInterpreterAttachments(fileIds);

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