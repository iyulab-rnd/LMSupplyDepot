using System.Text.Json;

namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Extension methods for the Run class
/// </summary>
public static class RunExtensions
{
    /// <summary>
    /// Gets the last error for this run
    /// </summary>
    public static RunError GetLastError(this Run run)
    {
        if (run == null || run.Status != RunStatus.Failed)
            return null;

        if (!run.HasProperty("last_error"))
            return null;

        return run.GetValue<RunError>("last_error");
    }

    /// <summary>
    /// Checks if the run requires tool outputs submission
    /// </summary>
    public static bool RequiresToolOutputs(this Run run)
    {
        var requiredAction = run.GetRequiredAction();
        return requiredAction != null && requiredAction.Type == RequiredActionTypes.SubmitToolOutputs;
    }

    /// <summary>
    /// Gets the tool calls that need outputs to be submitted
    /// </summary>
    public static List<RunToolCall> GetRequiredToolCalls(this Run run)
    {
        var requiredAction = run.GetRequiredAction();
        if (requiredAction == null || requiredAction.Type != RequiredActionTypes.SubmitToolOutputs)
            return new List<RunToolCall>();

        if (requiredAction.SubmitToolOutputs == null)
            return new List<RunToolCall>();

        return requiredAction.SubmitToolOutputs.ToolCalls ?? new List<RunToolCall>();
    }

    /// <summary>
    /// Gets the function tool calls that need outputs to be submitted
    /// </summary>
    public static List<(string Id, string FunctionName, string Arguments)> GetRequiredFunctionCalls(this Run run)
    {
        var toolCalls = run.GetRequiredToolCalls();
        var functionCalls = new List<(string Id, string FunctionName, string Arguments)>();

        foreach (var toolCall in toolCalls)
        {
            if (toolCall.Type == ToolTypes.Function && toolCall.Function != null)
            {
                functionCalls.Add((toolCall.Id, toolCall.Function.Name, toolCall.Function.Arguments));
            }
        }

        return functionCalls;
    }
}