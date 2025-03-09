namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Base class for request models with dynamic property support
/// </summary>
public class BaseRequest : BaseModel
{
}

/// <summary>
/// Request model for creating an Assistant
/// </summary>
public class CreateAssistantRequest : BaseRequest
{
    /// <summary>
    /// ID of the model to use
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// Creates a new CreateAssistantRequest with the specified model
    /// </summary>
    public static CreateAssistantRequest Create(string model)
    {
        return new CreateAssistantRequest { Model = model };
    }

    /// <summary>
    /// Sets the model to use
    /// </summary>
    public CreateAssistantRequest WithModel(string model)
    {
        Model = model;
        return this;
    }

    /// <summary>
    /// Sets the name of the assistant
    /// </summary>
    public CreateAssistantRequest WithName(string name)
    {
        SetValue(PropertyNames.Name, name);
        return this;
    }

    /// <summary>
    /// Sets the description of the assistant
    /// </summary>
    public CreateAssistantRequest WithDescription(string description)
    {
        SetValue(PropertyNames.Description, description);
        return this;
    }

    /// <summary>
    /// Sets the instructions for the assistant
    /// </summary>
    public CreateAssistantRequest WithInstructions(string instructions)
    {
        SetValue(PropertyNames.Instructions, instructions);
        return this;
    }

    /// <summary>
    /// Sets the tools for the assistant
    /// </summary>
    public CreateAssistantRequest WithTools(List<Tool> tools)
    {
        SetValue(PropertyNames.Tools, tools);
        return this;
    }

    /// <summary>
    /// Sets the tool resources for the assistant
    /// </summary>
    public CreateAssistantRequest WithToolResources(object toolResources)
    {
        SetValue(PropertyNames.ToolResources, toolResources);
        return this;
    }

    /// <summary>
    /// Sets the metadata for the assistant
    /// </summary>
    public CreateAssistantRequest WithMetadata(Dictionary<string, string> metadata)
    {
        SetValue(PropertyNames.Metadata, metadata);
        return this;
    }
}

/// <summary>
/// Request model for updating an Assistant
/// </summary>
public class UpdateAssistantRequest : BaseRequest
{
    /// <summary>
    /// Creates a new UpdateAssistantRequest
    /// </summary>
    public static UpdateAssistantRequest Create()
    {
        return new UpdateAssistantRequest();
    }

    /// <summary>
    /// Sets the model to use
    /// </summary>
    public UpdateAssistantRequest WithModel(string model)
    {
        SetValue(PropertyNames.Model, model);
        return this;
    }

    /// <summary>
    /// Sets the name of the assistant
    /// </summary>
    public UpdateAssistantRequest WithName(string name)
    {
        SetValue(PropertyNames.Name, name);
        return this;
    }

    /// <summary>
    /// Sets the description of the assistant
    /// </summary>
    public UpdateAssistantRequest WithDescription(string description)
    {
        SetValue(PropertyNames.Description, description);
        return this;
    }

    /// <summary>
    /// Sets the instructions for the assistant
    /// </summary>
    public UpdateAssistantRequest WithInstructions(string instructions)
    {
        SetValue(PropertyNames.Instructions, instructions);
        return this;
    }

    /// <summary>
    /// Sets the tools for the assistant
    /// </summary>
    public UpdateAssistantRequest WithTools(List<Tool> tools)
    {
        SetValue(PropertyNames.Tools, tools);
        return this;
    }

    /// <summary>
    /// Sets the tool resources for the assistant
    /// </summary>
    public UpdateAssistantRequest WithToolResources(object toolResources)
    {
        SetValue(PropertyNames.ToolResources, toolResources);
        return this;
    }

    /// <summary>
    /// Sets the metadata for the assistant
    /// </summary>
    public UpdateAssistantRequest WithMetadata(Dictionary<string, string> metadata)
    {
        SetValue(PropertyNames.Metadata, metadata);
        return this;
    }
}

/// <summary>
/// Request model for creating a Thread
/// </summary>
public class CreateThreadRequest : BaseRequest
{
    /// <summary>
    /// Creates a new CreateThreadRequest
    /// </summary>
    public static CreateThreadRequest Create()
    {
        return new CreateThreadRequest();
    }

    /// <summary>
    /// Sets the messages to start the thread with
    /// </summary>
    public CreateThreadRequest WithMessages(List<CreateMessageRequest> messages)
    {
        SetValue("messages", messages);
        return this;
    }

    /// <summary>
    /// Sets the metadata for the thread
    /// </summary>
    public CreateThreadRequest WithMetadata(Dictionary<string, string> metadata)
    {
        SetValue(PropertyNames.Metadata, metadata);
        return this;
    }

    /// <summary>
    /// Sets the tool resources for the thread
    /// </summary>
    public CreateThreadRequest WithToolResources(object toolResources)
    {
        SetValue(PropertyNames.ToolResources, toolResources);
        return this;
    }
}

/// <summary>
/// Request model for updating a Thread
/// </summary>
public class UpdateThreadRequest : BaseRequest
{
    /// <summary>
    /// Creates a new UpdateThreadRequest
    /// </summary>
    public static UpdateThreadRequest Create()
    {
        return new UpdateThreadRequest();
    }

    /// <summary>
    /// Sets the metadata for the thread
    /// </summary>
    public UpdateThreadRequest WithMetadata(Dictionary<string, string> metadata)
    {
        SetValue(PropertyNames.Metadata, metadata);
        return this;
    }

    /// <summary>
    /// Sets the tool resources for the thread
    /// </summary>
    public UpdateThreadRequest WithToolResources(object toolResources)
    {
        SetValue(PropertyNames.ToolResources, toolResources);
        return this;
    }
}

/// <summary>
/// Request model for creating a Message
/// </summary>
public class CreateMessageRequest : BaseRequest
{
    /// <summary>
    /// The role of the entity creating the message. Currently only "user" is supported.
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = "user";

    /// <summary>
    /// The content of the message.
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; }

    /// <summary>
    /// Creates a new CreateMessageRequest with the specified content
    /// </summary>
    public static CreateMessageRequest Create(string content)
    {
        return new CreateMessageRequest { Content = content };
    }

    /// <summary>
    /// Sets the file IDs for the message
    /// </summary>
    public CreateMessageRequest WithFileIds(List<string> fileIds)
    {
        SetValue(PropertyNames.FileIds, fileIds);
        return this;
    }

    /// <summary>
    /// Sets the metadata for the message
    /// </summary>
    public CreateMessageRequest WithMetadata(Dictionary<string, string> metadata)
    {
        SetValue(PropertyNames.Metadata, metadata);
        return this;
    }

    /// <summary>
    /// Sets the attachments for the message
    /// </summary>
    public CreateMessageRequest WithAttachments(List<object> attachments)
    {
        SetValue(PropertyNames.Attachments, attachments);
        return this;
    }
}

/// <summary>
/// Request model for creating a Run
/// </summary>
public class CreateRunRequest : BaseRequest
{
    /// <summary>
    /// The ID of the assistant to use to execute this run
    /// </summary>
    [JsonPropertyName("assistant_id")]
    public string AssistantId { get; set; }

    /// <summary>
    /// Creates a new CreateRunRequest with the specified assistant ID
    /// </summary>
    public static CreateRunRequest Create(string assistantId)
    {
        return new CreateRunRequest { AssistantId = assistantId };
    }

    /// <summary>
    /// Sets the model to use for this run
    /// </summary>
    public CreateRunRequest WithModel(string model)
    {
        SetValue(PropertyNames.Model, model);
        return this;
    }

    /// <summary>
    /// Sets the instructions for this run
    /// </summary>
    public CreateRunRequest WithInstructions(string instructions)
    {
        SetValue(PropertyNames.Instructions, instructions);
        return this;
    }

    /// <summary>
    /// Sets the tools for this run
    /// </summary>
    public CreateRunRequest WithTools(List<Tool> tools)
    {
        SetValue(PropertyNames.Tools, tools);
        return this;
    }

    /// <summary>
    /// Sets the metadata for this run
    /// </summary>
    public CreateRunRequest WithMetadata(Dictionary<string, string> metadata)
    {
        SetValue(PropertyNames.Metadata, metadata);
        return this;
    }

    /// <summary>
    /// Sets the maximum number of tokens allowed for this run
    /// </summary>
    public CreateRunRequest WithMaxPromptTokens(int maxPromptTokens)
    {
        SetValue(PropertyNames.MaxPromptTokens, maxPromptTokens);
        return this;
    }

    /// <summary>
    /// Sets the maximum number of completion tokens allowed for this run
    /// </summary>
    public CreateRunRequest WithMaxCompletionTokens(int maxCompletionTokens)
    {
        SetValue(PropertyNames.MaxCompletionTokens, maxCompletionTokens);
        return this;
    }

    /// <summary>
    /// Sets the truncation strategy for this run
    /// </summary>
    public CreateRunRequest WithTruncationStrategy(object truncationStrategy)
    {
        SetValue(PropertyNames.TruncationStrategy, truncationStrategy);
        return this;
    }
}

/// <summary>
/// Request model for submitting tool outputs to a Run
/// </summary>
public class SubmitToolOutputsRequest : BaseRequest
{
    /// <summary>
    /// Creates a new SubmitToolOutputsRequest with the specified tool outputs
    /// </summary>
    public static SubmitToolOutputsRequest Create(List<ToolOutput> toolOutputs)
    {
        var request = new SubmitToolOutputsRequest();
        request.SetValue("tool_outputs", toolOutputs);
        return request;
    }
}

/// <summary>
/// Request model for creating a Vector Store
/// </summary>
public class CreateVectorStoreRequest : BaseRequest
{
    /// <summary>
    /// The name of the vector store
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Creates a new CreateVectorStoreRequest with the specified name
    /// </summary>
    public static CreateVectorStoreRequest Create(string name)
    {
        return new CreateVectorStoreRequest { Name = name };
    }

    /// <summary>
    /// Sets the file IDs for the vector store
    /// </summary>
    public CreateVectorStoreRequest WithFileIds(List<string> fileIds)
    {
        SetValue("file_ids", fileIds);
        return this;
    }

    /// <summary>
    /// Sets the expiration policy for the vector store
    /// </summary>
    public CreateVectorStoreRequest WithExpiresAfter(object expiresAfter)
    {
        SetValue("expires_after", expiresAfter);
        return this;
    }
}

/// <summary>
/// Request model for updating a Vector Store
/// </summary>
public class UpdateVectorStoreRequest : BaseRequest
{
    /// <summary>
    /// Creates a new UpdateVectorStoreRequest
    /// </summary>
    public static UpdateVectorStoreRequest Create()
    {
        return new UpdateVectorStoreRequest();
    }

    /// <summary>
    /// Sets the name of the vector store
    /// </summary>
    public UpdateVectorStoreRequest WithName(string name)
    {
        SetValue("name", name);
        return this;
    }

    /// <summary>
    /// Sets the expiration policy for the vector store
    /// </summary>
    public UpdateVectorStoreRequest WithExpiresAfter(object expiresAfter)
    {
        SetValue("expires_after", expiresAfter);
        return this;
    }
}

/// <summary>
/// Request model for creating a Vector Store file batch
/// </summary>
public class CreateVectorStoreFileBatchRequest : BaseRequest
{
    /// <summary>
    /// Creates a new CreateVectorStoreFileBatchRequest with the specified file IDs
    /// </summary>
    public static CreateVectorStoreFileBatchRequest Create(List<string> fileIds)
    {
        var request = new CreateVectorStoreFileBatchRequest();
        request.SetValue("file_ids", fileIds);
        return request;
    }
}

/// <summary>
/// Request model for creating a Vector Store file
/// </summary>
public class CreateVectorStoreFileRequest : BaseRequest
{
    /// <summary>
    /// The ID of the file to add to the vector store
    /// </summary>
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }

    /// <summary>
    /// Creates a new CreateVectorStoreFileRequest with the specified file ID
    /// </summary>
    public static CreateVectorStoreFileRequest Create(string fileId)
    {
        return new CreateVectorStoreFileRequest { FileId = fileId };
    }

    /// <summary>
    /// Sets the chunking strategy for the file
    /// </summary>
    public CreateVectorStoreFileRequest WithChunkingStrategy(int maxChunkSizeTokens, int chunkOverlapTokens)
    {
        var chunkingStrategy = new Dictionary<string, object>
        {
            { "max_chunk_size_tokens", maxChunkSizeTokens },
            { "chunk_overlap_tokens", chunkOverlapTokens }
        };

        SetValue("chunking_strategy", chunkingStrategy);
        return this;
    }
}