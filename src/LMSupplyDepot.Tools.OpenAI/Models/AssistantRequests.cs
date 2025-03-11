namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Request model for creating an Assistant
/// </summary>
public class CreateAssistantRequest : BaseRequest
{
    /// <summary>
    /// ID of the model to use
    /// </summary>
    [JsonPropertyName("model")]
    public string Model { get; set; }

    /// <summary>
    /// Creates a new CreateAssistantRequest with the specified model
    /// </summary>
    public static CreateAssistantRequest Create(string model)
    {
        return new CreateAssistantRequest { Model = model };
    }

    /// <summary>
    /// Sets the model to use
    /// </summary>
    public CreateAssistantRequest WithModel(string model)
    {
        Model = model;
        return this;
    }

    /// <summary>
    /// Sets the name of the assistant
    /// </summary>
    public CreateAssistantRequest WithName(string name)
    {
        SetValue(PropertyNames.Name, name);
        return this;
    }

    /// <summary>
    /// Sets the description of the assistant
    /// </summary>
    public CreateAssistantRequest WithDescription(string description)
    {
        SetValue(PropertyNames.Description, description);
        return this;
    }

    /// <summary>
    /// Sets the instructions for the assistant
    /// </summary>
    public CreateAssistantRequest WithInstructions(string instructions)
    {
        SetValue(PropertyNames.Instructions, instructions);
        return this;
    }

    /// <summary>
    /// Sets the tools for the assistant
    /// </summary>
    public CreateAssistantRequest WithTools(List<Tool> tools)
    {
        SetValue(PropertyNames.Tools, tools);
        return this;
    }

    /// <summary>
    /// Sets the tool resources for the assistant
    /// </summary>
    public CreateAssistantRequest WithToolResources(object toolResources)
    {
        SetValue(PropertyNames.ToolResources, toolResources);
        return this;
    }

    /// <summary>
    /// Sets the metadata for the assistant
    /// </summary>
    public CreateAssistantRequest WithMetadata(Dictionary<string, string> metadata)
    {
        SetValue(PropertyNames.Metadata, metadata);
        return this;
    }
}

/// <summary>
/// Request model for updating an Assistant
/// </summary>
public class UpdateAssistantRequest : BaseRequest
{
    /// <summary>
    /// Creates a new UpdateAssistantRequest
    /// </summary>
    public static UpdateAssistantRequest Create()
    {
        return new UpdateAssistantRequest();
    }

    /// <summary>
    /// Sets the model to use
    /// </summary>
    public UpdateAssistantRequest WithModel(string model)
    {
        SetValue(PropertyNames.Model, model);
        return this;
    }

    /// <summary>
    /// Sets the name of the assistant
    /// </summary>
    public UpdateAssistantRequest WithName(string name)
    {
        SetValue(PropertyNames.Name, name);
        return this;
    }

    /// <summary>
    /// Sets the description of the assistant
    /// </summary>
    public UpdateAssistantRequest WithDescription(string description)
    {
        SetValue(PropertyNames.Description, description);
        return this;
    }

    /// <summary>
    /// Sets the instructions for the assistant
    /// </summary>
    public UpdateAssistantRequest WithInstructions(string instructions)
    {
        SetValue(PropertyNames.Instructions, instructions);
        return this;
    }

    /// <summary>
    /// Sets the tools for the assistant
    /// </summary>
    public UpdateAssistantRequest WithTools(List<Tool> tools)
    {
        SetValue(PropertyNames.Tools, tools);
        return this;
    }

    /// <summary>
    /// Sets the tool resources for the assistant
    /// </summary>
    public UpdateAssistantRequest WithToolResources(object toolResources)
    {
        SetValue(PropertyNames.ToolResources, toolResources);
        return this;
    }

    /// <summary>
    /// Sets the metadata for the assistant
    /// </summary>
    public UpdateAssistantRequest WithMetadata(Dictionary<string, string> metadata)
    {
        SetValue(PropertyNames.Metadata, metadata);
        return this;
    }
}