namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents an error that occurred during a run
/// </summary>
public class RunError : BaseModel
{
    /// <summary>
    /// The error code
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; }

    /// <summary>
    /// The error message
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; }
}

/// <summary>
/// Constants for run error codes
/// </summary>
public static class RunErrorCodes
{
    /// <summary>
    /// The run timed out
    /// </summary>
    public const string ServerError = "server_error";

    /// <summary>
    /// The rate limit was exceeded
    /// </summary>
    public const string RateLimitExceeded = "rate_limit_exceeded";

    /// <summary>
    /// The run was manually cancelled
    /// </summary>
    public const string RunCancelled = "run_cancelled";

    /// <summary>
    /// The maximum number of tokens was exceeded
    /// </summary>
    public const string MaxTokensExceeded = "max_tokens_exceeded";

    /// <summary>
    /// Required tool outputs were not submitted in time
    /// </summary>
    public const string ToolOutputsRequired = "tool_outputs_required";

    /// <summary>
    /// Content was filtered due to OpenAI's content policy
    /// </summary>
    public const string ContentFiltered = "content_filtered";
}