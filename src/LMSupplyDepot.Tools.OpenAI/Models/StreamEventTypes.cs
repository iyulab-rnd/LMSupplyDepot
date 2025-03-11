namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Constants for streaming event types
/// </summary>
public static class StreamEventTypes
{
    // Thread 이벤트
    public const string ThreadCreated = "thread.created";

    // Run 이벤트
    public const string ThreadRunCreated = "thread.run.created";
    public const string ThreadRunQueued = "thread.run.queued";
    public const string ThreadRunInProgress = "thread.run.in_progress";
    public const string ThreadRunRequiresAction = "thread.run.requires_action";
    public const string ThreadRunCompleted = "thread.run.completed";
    public const string ThreadRunFailed = "thread.run.failed";
    public const string ThreadRunCancelling = "thread.run.cancelling";
    public const string ThreadRunCancelled = "thread.run.cancelled";
    public const string ThreadRunExpired = "thread.run.expired";

    // Message 이벤트
    public const string ThreadMessageCreated = "thread.message.created";
    public const string ThreadMessageInProgress = "thread.message.in_progress";
    public const string ThreadMessageCompleted = "thread.message.completed";
    public const string ThreadMessageIncomplete = "thread.message.incomplete";
    public const string ThreadMessageDelta = "thread.message.delta";

    // Run Step 이벤트
    public const string ThreadRunStepCreated = "thread.run.step.created";
    public const string ThreadRunStepInProgress = "thread.run.step.in_progress";
    public const string ThreadRunStepCompleted = "thread.run.step.completed";
    public const string ThreadRunStepFailed = "thread.run.step.failed";
    public const string ThreadRunStepCancelled = "thread.run.step.cancelled";
    public const string ThreadRunStepExpired = "thread.run.step.expired";
    public const string ThreadRunStepDelta = "thread.run.step.delta";

    // 스트림 완료 이벤트
    public const string Done = "done";

    // 오류 이벤트
    public const string Error = "error";
}