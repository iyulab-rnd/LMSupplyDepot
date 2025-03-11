namespace LMSupplyDepot.Tools.OpenAI.Models;

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