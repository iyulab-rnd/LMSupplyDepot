namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Class representing a Run Step delta event
/// </summary>
public class RunStepDelta : BaseModel
{
    /// <summary>
    /// Run Step ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Object type (always "thread.run.step.delta")
    /// </summary>
    [JsonPropertyName("object")]
    public string Object { get; set; }

    /// <summary>
    /// Delta content
    /// </summary>
    [JsonPropertyName("delta")]
    public RunStepDeltaContent Delta { get; set; }
}

/// <summary>
/// Class representing Run Step delta content
/// </summary>
public class RunStepDeltaContent : BaseModel
{
    /// <summary>
    /// Step details changes
    /// </summary>
    [JsonPropertyName("step_details")]
    public StepDetailsDelta StepDetails { get; set; }
}

/// <summary>
/// Class representing step details delta
/// </summary>
public class StepDetailsDelta : BaseModel
{
    /// <summary>
    /// Details type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Tool call information (if type is "tool_calls")
    /// </summary>
    [JsonPropertyName("tool_calls")]
    public List<ToolCallDelta> ToolCalls { get; set; }
}

/// <summary>
/// Class representing a tool call delta
/// </summary>
public class ToolCallDelta : BaseModel
{
    /// <summary>
    /// Call index
    /// </summary>
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>
    /// Call ID
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// Call type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Code interpreter information (if type is "code_interpreter")
    /// </summary>
    [JsonPropertyName("code_interpreter")]
    public CodeInterpreterDelta CodeInterpreter { get; set; }

    /// <summary>
    /// Function call information (if type is "function")
    /// </summary>
    [JsonPropertyName("function")]
    public FunctionDelta Function { get; set; }
}

/// <summary>
/// Class representing a code interpreter delta
/// </summary>
public class CodeInterpreterDelta : BaseModel
{
    /// <summary>
    /// Input code
    /// </summary>
    [JsonPropertyName("input")]
    public string Input { get; set; }

    /// <summary>
    /// Output results
    /// </summary>
    [JsonPropertyName("outputs")]
    public List<CodeInterpreterOutput> Outputs { get; set; }
}

/// <summary>
/// Class representing image output
/// </summary>
public class ImageOutput : BaseModel
{
    /// <summary>
    /// Image MIME type
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Image data (base64 encoded)
    /// </summary>
    [JsonPropertyName("data")]
    public string Data { get; set; }
}

/// <summary>
/// Class representing a function call delta
/// </summary>
public class FunctionDelta : BaseModel
{
    /// <summary>
    /// Function name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Function arguments (JSON string)
    /// </summary>
    [JsonPropertyName("arguments")]
    public string Arguments { get; set; }
}