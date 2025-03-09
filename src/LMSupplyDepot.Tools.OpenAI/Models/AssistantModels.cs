namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Constants for property names used in the Assistants API
/// </summary>
public static class PropertyNames
{
    // Common properties
    public const string Id = "id";
    public const string Object = "object";
    public const string CreatedAt = "created_at";
    public const string Metadata = "metadata";

    // Assistant properties
    public const string Name = "name";
    public const string Description = "description";
    public const string Model = "model";
    public const string Instructions = "instructions";
    public const string Tools = "tools";
    public const string ToolResources = "tool_resources";

    // Thread properties
    public const string ThreadId = "thread_id";

    // Message properties
    public const string Role = "role";
    public const string Content = "content";
    public const string AssistantId = "assistant_id";
    public const string RunId = "run_id";
    public const string FileIds = "file_ids";
    public const string Attachments = "attachments";

    // Run properties
    public const string Status = "status";
    public const string ExpiresAt = "expires_at";
    public const string StartedAt = "started_at";
    public const string CancelledAt = "cancelled_at";
    public const string FailedAt = "failed_at";
    public const string CompletedAt = "completed_at";
    public const string LastError = "last_error";
    public const string RequiredAction = "required_action";
    public const string TruncationStrategy = "truncation_strategy";
    public const string MaxPromptTokens = "max_prompt_tokens";
    public const string MaxCompletionTokens = "max_completion_tokens";

    // Tool properties
    public const string Type = "type";
    public const string Function = "function";
    public const string FileSearch = "file_search";
    public const string CodeInterpreter = "code_interpreter";
}

/// <summary>
/// Constants for object types in the Assistants API
/// </summary>
public static class ObjectTypes
{
    public const string Assistant = "assistant";
    public const string Thread = "thread";
    public const string Message = "thread.message";
    public const string Run = "thread.run";
    public const string RunStep = "thread.run.step";
    public const string List = "list";
    public const string VectorStore = "vector_store";
}

/// <summary>
/// Constants for run status values in the Assistants API
/// </summary>
public static class RunStatus
{
    public const string Queued = "queued";
    public const string InProgress = "in_progress";
    public const string RequiresAction = "requires_action";
    public const string Cancelling = "cancelling";
    public const string Cancelled = "cancelled";
    public const string Failed = "failed";
    public const string Completed = "completed";
    public const string Expired = "expired";
    public const string Incomplete = "incomplete";
}

/// <summary>
/// Represents an object in the Assistants API with basic properties
/// </summary>
public class AssistantObject : BaseModel
{
    /// <summary>
    /// The identifier, which can be referenced in API endpoints
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The object type
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// The Unix timestamp (in seconds) for when the object was created
    /// </summary>
    [JsonPropertyName("created_at")]
    public int CreatedAt { get; set; }
}

/// <summary>
/// Represents an Assistant in the OpenAI Assistants API
/// </summary>
public class Assistant : AssistantObject
{
    /// <summary>
    /// The name of the assistant
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The description of the assistant
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; }

    /// <summary>
    /// ID of the model to use
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// The system instructions that the assistant uses
    /// </summary>
    [JsonPropertyName("instructions")]
    public string Instructions { get; set; }

    /// <summary>
    /// Set of key-value pairs that can be attached to the object
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// Get the tools associated with this assistant
    /// </summary>
    public List<Tool> GetTools()
    {
        return GetValue<List<Tool>>(PropertyNames.Tools);
    }

    /// <summary>
    /// Set the tools for this assistant
    /// </summary>
        public void SetTools(List<Tool> tools)
    {
        SetValue(PropertyNames.Tools, tools);
    }

    /// <summary>
    /// Get the tool resources for this assistant
    /// </summary>
    public object GetToolResources()
    {
        return GetValue<object>(PropertyNames.ToolResources);
    }

    /// <summary>
    /// Set the tool resources for this assistant
    /// </summary>
        public void SetToolResources(object toolResources)
    {
        SetValue(PropertyNames.ToolResources, toolResources);
    }
}

/// <summary>
/// Represents a Thread in the Assistants API
/// </summary>
public class ChatThread : AssistantObject
{
    /// <summary>
    /// Set of key-value pairs that can be attached to the object
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// Get the tool resources for this thread
    /// </summary>
    public object GetToolResources()
    {
        return GetValue<object>(PropertyNames.ToolResources);
    }

    /// <summary>
    /// Set the tool resources for this thread
    /// </summary>
        public void SetToolResources(object toolResources)
    {
        SetValue(PropertyNames.ToolResources, toolResources);
    }
}

/// <summary>
/// Represents a Message in the Assistants API
/// </summary>
public class Message : AssistantObject
{
    /// <summary>
    /// The thread ID that this message belongs to
    /// </summary>
    [JsonPropertyName("thread_id")]
    public string ThreadId { get; set; }

    /// <summary>
    /// The entity that produced the message. One of "user" or "assistant"
    /// </summary>
    [JsonPropertyName("role")]
    public string Role { get; set; }

    /// <summary>
    /// The content of the message in different formats
    /// </summary>
    [JsonPropertyName("content")]
    public List<MessageContent> Content { get; set; }

    /// <summary>
    /// If applicable, the ID of the assistant that authored this message
    /// </summary>
    [JsonPropertyName("assistant_id")]
    public string AssistantId { get; set; }

    /// <summary>
    /// If applicable, the ID of the run associated with the authoring of this message
    /// </summary>
    [JsonPropertyName("run_id")]
    public string RunId { get; set; }

    /// <summary>
    /// A list of file IDs that the message has access to
    /// </summary>
    [JsonPropertyName("file_ids")]
    public List<string> FileIds { get; set; }

    /// <summary>
    /// Set of key-value pairs that can be attached to the object
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// Get the attachments of the message
    /// </summary>
    public List<object> GetAttachments()
    {
        return GetValue<List<object>>(PropertyNames.Attachments);
    }

    /// <summary>
    /// Set the attachments for this message
    /// </summary>
        public void SetAttachments(List<object> attachments)
    {
        SetValue(PropertyNames.Attachments, attachments);
    }
}

/// <summary>
/// Represents the content of a message
/// </summary>
public class MessageContent : BaseModel
{
    /// <summary>
    /// The type of content. Can be "text", "image_file", etc.
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Get the text content if available
    /// </summary>
    public TextContent GetTextContent()
    {
        return GetValue<TextContent>("text");
    }

    /// <summary>
    /// Get the image file content if available
    /// </summary>
    public ImageFileContent GetImageFileContent()
    {
        return GetValue<ImageFileContent>("image_file");
    }
}

/// <summary>
/// Represents text content
/// </summary>
public class TextContent : BaseModel
{
    /// <summary>
    /// The text value
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; }

    /// <summary>
    /// Get the annotations within the text
    /// </summary>
    public List<Annotation> GetAnnotations()
    {
        return GetValue<List<Annotation>>("annotations");
    }
}

/// <summary>
/// Represents an annotation within text content
/// </summary>
public class Annotation : BaseModel
{
    /// <summary>
    /// Type of annotation
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// The text that is being annotated
    /// </summary>
    [JsonPropertyName("text")]
    public string Text { get; set; }

    /// <summary>
    /// Start index of the annotation in the text
    /// </summary>
    [JsonPropertyName("start_index")]
    public int StartIndex { get; set; }

    /// <summary>
    /// End index of the annotation in the text
    /// </summary>
    [JsonPropertyName("end_index")]
    public int EndIndex { get; set; }
}

/// <summary>
/// Represents an image file content
/// </summary>
public class ImageFileContent : BaseModel
{
    /// <summary>
    /// The ID of the image file
    /// </summary>
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }
}

/// <summary>
/// Represents a Run in the Assistants API
/// </summary>
public class Run : AssistantObject
{
    /// <summary>
    /// The ID of the thread that was executed on as a part of this run
    /// </summary>
    [JsonPropertyName("thread_id")]
    public string ThreadId { get; set; }

    /// <summary>
    /// The ID of the assistant used for execution of this run
    /// </summary>
    [JsonPropertyName("assistant_id")]
    public string AssistantId { get; set; }

    /// <summary>
    /// The status of the run
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// The model that the assistant used for this run
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// The instructions that the assistant used for this run
    /// </summary>
    [JsonPropertyName("instructions")]
    public string Instructions { get; set; }

    /// <summary>
    /// Set of key-value pairs that can be attached to the object
    /// </summary>
    [JsonPropertyName("metadata")]
    public Dictionary<string, string> Metadata { get; set; }

    /// <summary>
    /// Get the required action for this run
    /// </summary>
    public object GetRequiredAction()
    {
        return GetValue<object>(PropertyNames.RequiredAction);
    }

    /// <summary>
    /// Get the tools for this run
    /// </summary>
    public List<Tool> GetTools()
    {
        return GetValue<List<Tool>>(PropertyNames.Tools);
    }

    /// <summary>
    /// Get the file IDs for this run
    /// </summary>
    public List<string> GetFileIds()
    {
        return GetValue<List<string>>(PropertyNames.FileIds);
    }
}

/// <summary>
/// Represents a Run Step in the Assistants API
/// </summary>
public class RunStep : AssistantObject
{
    /// <summary>
    /// The ID of the run that this run step is a part of
    /// </summary>
    [JsonPropertyName("run_id")]
    public string RunId { get; set; }

    /// <summary>
    /// The ID of the assistant associated with the run
    /// </summary>
    [JsonPropertyName("assistant_id")]
    public string AssistantId { get; set; }

    /// <summary>
    /// The ID of the thread that was executed on as a part of this run step
    /// </summary>
    [JsonPropertyName("thread_id")]
    public string ThreadId { get; set; }

    /// <summary>
    /// The type of run step
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// The status of the run step
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// Get the step details
    /// </summary>
    public object GetStepDetails()
    {
        return GetValue<object>("step_details");
    }
}

/// <summary>
/// Represents a Vector Store in the Assistants API
/// </summary>
public class VectorStore : AssistantObject
{
    /// <summary>
    /// The name of the vector store
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The status of the vector store
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// The size of the vector store in bytes
    /// </summary>
    [JsonPropertyName("bytes")]
    public long Bytes { get; set; }

    /// <summary>
    /// Gets the file counts for this vector store
    /// </summary>
    public VectorStoreFileCounts GetFileCounts()
    {
        return GetValue<VectorStoreFileCounts>("file_counts");
    }

    /// <summary>
    /// Gets the expiration policy for this vector store
    /// </summary>
    public object GetExpiresAfter()
    {
        return GetValue<object>("expires_after");
    }
}

/// <summary>
/// Represents a list response in the Assistants API
/// </summary>
/// <typeparam name="T">The type of objects in the list</typeparam>
public class ListResponse<T> : BaseModel
{
    /// <summary>
    /// The object type, which is always "list"
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// The list of objects
    /// </summary>
    [JsonPropertyName("data")]
    public List<T> Data { get; set; }

    /// <summary>
    /// The ID of the first object in the list
    /// </summary>
    [JsonPropertyName("first_id")]
    public string FirstId { get; set; }

    /// <summary>
    /// The ID of the last object in the list
    /// </summary>
    [JsonPropertyName("last_id")]
    public string LastId { get; set; }

    /// <summary>
    /// Whether there are more objects available
    /// </summary>
    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }
}

/// <summary>
/// Represents a file citation in a message
/// </summary>
public class FileCitation : BaseModel
{
    /// <summary>
    /// The ID of the file being cited
    /// </summary>
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }

    /// <summary>
    /// The quoted text from the file
    /// </summary>
    [JsonPropertyName("quote")]
    public string Quote { get; set; }
}

/// <summary>
/// Represents a file path in a message
/// </summary>
public class FilePath : BaseModel
{
    /// <summary>
    /// The ID of the file being referenced
    /// </summary>
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }
}