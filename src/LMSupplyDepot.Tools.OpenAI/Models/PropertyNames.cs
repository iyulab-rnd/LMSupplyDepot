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