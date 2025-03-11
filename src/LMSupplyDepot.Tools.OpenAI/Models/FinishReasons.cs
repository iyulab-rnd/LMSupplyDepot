namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Constants for finish reasons
/// </summary>
public static class FinishReasons
{
    public const string Stop = "stop";
    public const string Length = "length";
    public const string ToolCalls = "tool_calls";
    public const string ContentFilter = "content_filter";
    public const string FunctionCall = "function_call";
}