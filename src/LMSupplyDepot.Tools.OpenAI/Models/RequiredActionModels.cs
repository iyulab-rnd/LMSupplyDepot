namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Types of required actions
/// </summary>
public static class RequiredActionTypes
{
    /// <summary>
    /// The run requires the caller to submit tool outputs
    /// </summary>
    public const string SubmitToolOutputs = "submit_tool_outputs";
}

/// <summary>
/// Represents a required action for a run
/// </summary>
public class RequiredAction : BaseModel
{
    /// <summary>
    /// The type of required action
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// The submit tool outputs action if this is a submit_tool_outputs action
    /// </summary>
    [JsonPropertyName("submit_tool_outputs")]
    public SubmitToolOutputsAction SubmitToolOutputs { get; set; }
}

/// <summary>
/// Represents a submit tool outputs action
/// </summary>
public class SubmitToolOutputsAction : BaseModel
{
    /// <summary>
    /// The tool calls that need to be submitted
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public List<RunToolCall> ToolCalls { get; set; } = new List<RunToolCall>();
}

/// <summary>
/// Represents a tool call in a run
/// </summary>
public class RunToolCall : BaseModel
{
    /// <summary>
    /// The ID of the tool call
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// The type of tool call
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// The function details if this is a function tool call
    /// </summary>
    [JsonPropertyName("function")]
    public FunctionToolCall Function { get; set; }

    /// <summary>
    /// The code interpreter details if this is a code interpreter tool call
    /// </summary>
    [JsonPropertyName("code_interpreter")]
    public CodeInterpreterToolCall CodeInterpreter { get; set; }

    /// <summary>
    /// The retrieval details if this is a retrieval tool call
    /// </summary>
    [JsonPropertyName("retrieval")]
    public RetrievalToolCall Retrieval { get; set; }
}

/// <summary>
/// Represents a function tool call
/// </summary>
public class FunctionToolCall : BaseModel
{
    /// <summary>
    /// The name of the function to call
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// The arguments to pass to the function
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; }
}

/// <summary>
/// Represents a code interpreter tool call
/// </summary>
public class CodeInterpreterToolCall : BaseModel
{
    /// <summary>
    /// The input to the code interpreter
    /// </summary>
    [JsonPropertyName("input")]
    public string Input { get; set; }

    /// <summary>
    /// The outputs from the code interpreter
    /// </summary>
    [JsonPropertyName("outputs")]
    public List<CodeInterpreterOutput> Outputs { get; set; } = new List<CodeInterpreterOutput>();
}

/// <summary>
/// Represents a retrieval tool call
/// </summary>
public class RetrievalToolCall : BaseModel
{
    // The retrieval tool call doesn't have any additional properties
}

/// <summary>
/// Represents an output from the code interpreter
/// </summary>
public class CodeInterpreterOutput : BaseModel
{
    /// <summary>
    /// The type of output
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// The log output if type is "logs"
    /// </summary>
    [JsonPropertyName("logs")]
    public string Logs { get; set; }

    /// <summary>
    /// The image details if type is "image"
    /// </summary>
    [JsonPropertyName("image")]
    public CodeInterpreterImage Image { get; set; }
}

/// <summary>
/// Represents an image output from the code interpreter
/// </summary>
public class CodeInterpreterImage : BaseModel
{
    /// <summary>
    /// The MIME type of the image
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// The Base64-encoded image data
    /// </summary>
    [JsonPropertyName("data")]
    public string Data { get; set; }
}