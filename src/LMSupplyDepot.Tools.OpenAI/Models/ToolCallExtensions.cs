namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Extension methods for ToolCall
/// </summary>
public static class ToolCallExtensions
{
    /// <summary>
    /// Gets the function name and arguments from a function tool call
    /// </summary>
    public static (string Name, string Arguments) GetFunctionDetails(this ToolCall toolCall)
    {
        if (toolCall == null || toolCall.Type != ToolTypes.Function)
        {
            return (null, null);
        }

        var function = toolCall.GetFunction();
        if (function == null)
        {
            return (null, null);
        }

        string name = null;
        string args = null;

        if (function.ContainsKey("name"))
            name = function["name"].ToString();

        if (function.ContainsKey("arguments"))
            args = function["arguments"].ToString();

        return (name, args);
    }

    /// <summary>
    /// Tries to parse function arguments as a specific type
    /// </summary>
    public static bool TryParseFunctionArguments<T>(this ToolCall toolCall, out T result) where T : class
    {
        result = default;

        var (_, arguments) = toolCall.GetFunctionDetails();
        if (string.IsNullOrEmpty(arguments))
        {
            return false;
        }

        try
        {
            result = JsonSerializer.Deserialize<T>(arguments, new JsonSerializerOptions
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
    public static T ParseFunctionArguments<T>(this ToolCall toolCall) where T : class
    {
        var (_, arguments) = toolCall.GetFunctionDetails();
        if (string.IsNullOrEmpty(arguments))
        {
            throw new ArgumentException("Tool call has no arguments", nameof(toolCall));
        }

        try
        {
            var result = JsonSerializer.Deserialize<T>(arguments, new JsonSerializerOptions
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
}