namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Constants for tool types in the OpenAI API
/// </summary>
public static class ToolTypes
{
    public const string Function = "function";
    public const string CodeInterpreter = "code_interpreter";
    public const string FileSearch = "file_search";
}

/// <summary>
/// Represents a tool that can be used by models in the OpenAI API
/// </summary>
public class Tool : BaseModel
{
    /// <summary>
    /// The type of the tool
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Create a function tool
    /// </summary>
    public static Tool CreateFunctionTool(string name, string description, object parameters)
    {
        var tool = new Tool { Type = ToolTypes.Function };
        var function = new Dictionary<string, object>
        {
            { "name", name },
            { "description", description },
            { "parameters", parameters }
        };
        tool.SetValue("function", function);
        return tool;
    }

    /// <summary>
    /// Create a code interpreter tool
    /// </summary>
    public static Tool CreateCodeInterpreterTool()
    {
        return new Tool { Type = ToolTypes.CodeInterpreter };
    }

    /// <summary>
    /// Create a file search tool
    /// </summary>
    public static Tool CreateFileSearchTool(int? maxNumResults = null, string? ranker = null, double? scoreThreshold = null)
    {
        var tool = new Tool { Type = ToolTypes.FileSearch };

        if (maxNumResults.HasValue || !string.IsNullOrEmpty(ranker) || scoreThreshold.HasValue)
        {
            var fileSearchConfig = new Dictionary<string, object>();

            if (maxNumResults.HasValue)
            {
                fileSearchConfig["max_num_results"] = maxNumResults.Value;
            }

            if (!string.IsNullOrEmpty(ranker) || scoreThreshold.HasValue)
            {
                var rankingOptions = new Dictionary<string, object>();

                if (!string.IsNullOrEmpty(ranker))
                {
                    rankingOptions["ranker"] = ranker;
                }

                if (scoreThreshold.HasValue)
                {
                    rankingOptions["score_threshold"] = scoreThreshold.Value;
                }

                fileSearchConfig["ranking_options"] = rankingOptions;
            }

            tool.SetValue("file_search", fileSearchConfig);
        }

        return tool;
    }

    /// <summary>
    /// Get the function definition
    /// </summary>
    public Dictionary<string, object> GetFunction()
    {
        return GetValue<Dictionary<string, object>>("function");
    }

    /// <summary>
    /// Get the file search configuration
    /// </summary>
    public Dictionary<string, object> GetFileSearch()
    {
        return GetValue<Dictionary<string, object>>("file_search");
    }
}

/// <summary>
/// Represents a tool call in a completion response
/// </summary>
public class ToolCall : BaseModel
{
    /// <summary>
    /// The ID of the tool call
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The type of the tool call
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Gets the function call details
    /// </summary>
    public Dictionary<string, object> GetFunction()
    {
        return GetValue<Dictionary<string, object>>("function");
    }

    /// <summary>
    /// Gets the function name
    /// </summary>
    public string GetFunctionName()
    {
        var function = GetFunction();
        return function != null && function.ContainsKey("name") ? function["name"].ToString() : null;
    }

    /// <summary>
    /// Gets the function arguments as a string
    /// </summary>
    public string GetFunctionArguments()
    {
        var function = GetFunction();
        return function != null && function.ContainsKey("arguments") ? function["arguments"].ToString() : null;
    }
}

/// <summary>
/// Represents a tool output for submitting to a run
/// </summary>
public class ToolOutput : BaseModel
{
    /// <summary>
    /// The ID of the tool call
    /// </summary>
    [JsonPropertyName("tool_call_id")]
    public string ToolCallId { get; set; }

    /// <summary>
    /// The output of the tool call
    /// </summary>
    [JsonPropertyName("output")]
    public string Output { get; set; }

    /// <summary>
    /// Creates a new ToolOutput with the specified tool call ID and output
    /// </summary>
    public static ToolOutput Create(string toolCallId, string output)
    {
        return new ToolOutput
        {
            ToolCallId = toolCallId,
            Output = output
        };
    }
}