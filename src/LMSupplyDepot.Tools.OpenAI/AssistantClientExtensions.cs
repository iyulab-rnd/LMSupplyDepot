using LMSupplyDepot.Tools.OpenAI.Utilities;
using ChatThread = LMSupplyDepot.Tools.OpenAI.Models.ChatThread;

namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Extension methods for OpenAIAssistantsClient
/// </summary>
public static class AssistantClientExtensions
{
    /// <summary>
    /// Creates a message with file attachments for file search
    /// </summary>
    public static async Task<Message> CreateUserMessageWithFileSearchAsync(this OpenAIAssistantsClient client, string threadId, string content, List<string> fileIds, CancellationToken cancellationToken = default)
    {
        var request = CreateMessageRequest.Create(content);

        if (fileIds != null && fileIds.Count > 0)
        {
            var attachments = new List<object>();
            foreach (var fileId in fileIds)
            {
                var attachment = MessageAttachment.Create(fileId).WithFileSearchTool();
                attachments.Add(attachment);
            }

            request.WithAttachments(attachments);
        }

        return await client.CreateMessageAsync(threadId, request, cancellationToken);
    }

    /// <summary>
    /// Creates a thread with tool resources for vector stores
    /// </summary>
    public static async Task<ChatThread> CreateThreadWithVectorStoresAsync(this OpenAIAssistantsClient client, List<string> vectorStoreIds, List<CreateMessageRequest> messages = null, CancellationToken cancellationToken = default)
    {
        var request = CreateThreadRequest.Create();

        if (messages != null && messages.Count > 0)
        {
            request.WithMessages(messages);
        }

        if (vectorStoreIds != null && vectorStoreIds.Count > 0)
        {
            var toolResources = AssistantHelpers.CreateFileSearchToolResources(vectorStoreIds);
            request.WithToolResources(toolResources);
        }

        return await client.CreateThreadAsync(request, cancellationToken);
    }

    /// <summary>
    /// Updates an assistant with vector stores for file search
    /// </summary>
    public static async Task<Assistant> UpdateAssistantWithVectorStoresAsync(this OpenAIAssistantsClient client, string assistantId, List<string> vectorStoreIds, CancellationToken cancellationToken = default)
    {
        var request = UpdateAssistantRequest.Create();

        if (vectorStoreIds != null && vectorStoreIds.Count > 0)
        {
            var toolResources = AssistantHelpers.CreateFileSearchToolResources(vectorStoreIds);
            request.WithToolResources(toolResources);
        }

        return await client.UpdateAssistantAsync(assistantId, request, cancellationToken);
    }

    /// <summary>
    /// Creates an assistant with file search tool and vector stores
    /// </summary>
    public static async Task<Assistant> CreateAssistantWithFileSearchAsync(this OpenAIAssistantsClient client, string model, string instructions, List<string> vectorStoreIds, string? name = null, CancellationToken cancellationToken = default)
    {
        var request = CreateAssistantRequest.Create(model)
            .WithInstructions(instructions)
            .WithTools(new List<Tool> { Tool.CreateFileSearchTool() });

        if (!string.IsNullOrEmpty(name))
        {
            request.WithName(name);
        }

        if (vectorStoreIds != null && vectorStoreIds.Count > 0)
        {
            var toolResources = AssistantHelpers.CreateFileSearchToolResources(vectorStoreIds);
            request.SetValue("tool_resources", toolResources);
        }

        return await client.CreateAssistantAsync(request, cancellationToken);
    }
}