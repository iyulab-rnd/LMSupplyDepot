namespace LMSupplyDepot.Tools.OpenAI.Models;

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