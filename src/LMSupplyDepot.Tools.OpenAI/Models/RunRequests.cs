namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Request model for creating a Run
/// </summary>
public class CreateRunRequest : BaseRequest
{
    /// <summary>
    /// The ID of the assistant to use to execute this run
    /// </summary>
    [JsonPropertyName("assistant_id")]
    public string AssistantId { get; set; }

    /// <summary>
    /// Creates a new CreateRunRequest with the specified assistant ID
    /// </summary>
    public static CreateRunRequest Create(string assistantId)
    {
        return new CreateRunRequest { AssistantId = assistantId };
    }

    /// <summary>
    /// Sets the model to use for this run
    /// </summary>
    public CreateRunRequest WithModel(string model)
    {
        SetValue(PropertyNames.Model, model);
        return this;
    }

    /// <summary>
    /// Sets the instructions for this run
    /// </summary>
    public CreateRunRequest WithInstructions(string instructions)
    {
        SetValue(PropertyNames.Instructions, instructions);
        return this;
    }

    /// <summary>
    /// Sets the tools for this run
    /// </summary>
    public CreateRunRequest WithTools(List<Tool> tools)
    {
        SetValue(PropertyNames.Tools, tools);
        return this;
    }

    /// <summary>
    /// Sets the metadata for this run
    /// </summary>
    public CreateRunRequest WithMetadata(Dictionary<string, string> metadata)
    {
        SetValue(PropertyNames.Metadata, metadata);
        return this;
    }

    /// <summary>
    /// Sets the maximum number of tokens allowed for this run
    /// </summary>
    public CreateRunRequest WithMaxPromptTokens(int maxPromptTokens)
    {
        SetValue(PropertyNames.MaxPromptTokens, maxPromptTokens);
        return this;
    }

    /// <summary>
    /// Sets the maximum number of completion tokens allowed for this run
    /// </summary>
    public CreateRunRequest WithMaxCompletionTokens(int maxCompletionTokens)
    {
        SetValue(PropertyNames.MaxCompletionTokens, maxCompletionTokens);
        return this;
    }

    /// <summary>
    /// Sets the truncation strategy for this run
    /// </summary>
    public CreateRunRequest WithTruncationStrategy(object truncationStrategy)
    {
        SetValue(PropertyNames.TruncationStrategy, truncationStrategy);
        return this;
    }

    /// <summary>
    /// Enables streaming for this run
    /// </summary>
    public CreateRunRequest WithStream(bool stream)
    {
        SetValue("stream", stream);
        return this;
    }
}

/// <summary>
/// Request model for submitting tool outputs to a Run
/// </summary>
public class SubmitToolOutputsRequest : BaseRequest
{
    /// <summary>
    /// Creates a new SubmitToolOutputsRequest with the specified tool outputs
    /// </summary>
    public static SubmitToolOutputsRequest Create(List<ToolOutput> toolOutputs)
    {
        var request = new SubmitToolOutputsRequest();
        request.SetValue("tool_outputs", toolOutputs);
        return request;
    }
}