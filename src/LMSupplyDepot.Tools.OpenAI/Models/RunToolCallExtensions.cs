namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Extension methods for RunToolCall
/// </summary>
public static class RunToolCallExtensions
{
    /// <summary>
    /// Tries to parse function arguments as a specific type
    /// </summary>
    public static bool TryParseFunctionArguments<T>(this RunToolCall toolCall, out T result) where T : class
    {
        result = default;

        if (toolCall == null || toolCall.Type != ToolTypes.Function || toolCall.Function == null)
        {
            return false;
        }

        if (string.IsNullOrEmpty(toolCall.Function.Arguments))
        {
            return false;
        }

        try
        {
            result = JsonSerializer.Deserialize<T>(toolCall.Function.Arguments, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return result != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Parses function arguments as a specific type
    /// </summary>
    public static T ParseFunctionArguments<T>(this RunToolCall toolCall) where T : class
    {
        if (toolCall == null || toolCall.Type != ToolTypes.Function)
        {
            throw new ArgumentException("Tool call is not a function call", nameof(toolCall));
        }

        if (toolCall.Function == null || string.IsNullOrEmpty(toolCall.Function.Arguments))
        {
            throw new ArgumentException("Function call has no arguments", nameof(toolCall));
        }

        try
        {
            var result = JsonSerializer.Deserialize<T>(toolCall.Function.Arguments, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            if (result == null)
            {
                throw new JsonException("Deserialized result is null");
            }

            return result;
        }
        catch (JsonException ex)
        {
            throw new JsonException($"Failed to parse arguments as {typeof(T).Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the logs from a code interpreter tool call
    /// </summary>
    public static string GetCodeInterpreterLogs(this RunToolCall toolCall)
    {
        if (toolCall == null || toolCall.Type != ToolTypes.CodeInterpreter || toolCall.CodeInterpreter == null)
        {
            return null;
        }

        if (toolCall.CodeInterpreter.Outputs == null || toolCall.CodeInterpreter.Outputs.Count == 0)
        {
            return null;
        }

        // Find all log outputs
        var logs = toolCall.CodeInterpreter.Outputs
            .Where(o => o.Type == "logs" && !string.IsNullOrEmpty(o.Logs))
            .Select(o => o.Logs)
            .ToList();

        return string.Join("\n", logs);
    }

    /// <summary>
    /// Gets the images from a code interpreter tool call
    /// </summary>
    public static List<CodeInterpreterImage> GetCodeInterpreterImages(this RunToolCall toolCall)
    {
        if (toolCall == null || toolCall.Type != ToolTypes.CodeInterpreter || toolCall.CodeInterpreter == null)
        {
            return new List<CodeInterpreterImage>();
        }

        if (toolCall.CodeInterpreter.Outputs == null || toolCall.CodeInterpreter.Outputs.Count == 0)
        {
            return new List<CodeInterpreterImage>();
        }

        // Find all image outputs
        return toolCall.CodeInterpreter.Outputs
            .Where(o => o.Type == "image" && o.Image != null)
            .Select(o => o.Image)
            .ToList();
    }
}