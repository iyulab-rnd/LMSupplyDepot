namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Request model for creating a Thread
/// </summary>
public class CreateThreadRequest : BaseRequest
{
    /// <summary>
    /// Creates a new CreateThreadRequest
    /// </summary>
    public static CreateThreadRequest Create()
    {
        return new CreateThreadRequest();
    }

    /// <summary>
    /// Sets the messages to start the thread with
    /// </summary>
    public CreateThreadRequest WithMessages(List<CreateMessageRequest> messages)
    {
        SetValue("messages", messages);
        return this;
    }

    /// <summary>
    /// Sets the metadata for the thread
    /// </summary>
    public CreateThreadRequest WithMetadata(Dictionary<string, string> metadata)
    {
        SetValue(PropertyNames.Metadata, metadata);
        return this;
    }

    /// <summary>
    /// Sets the tool resources for the thread
    /// </summary>
    public CreateThreadRequest WithToolResources(object toolResources)
    {
        SetValue(PropertyNames.ToolResources, toolResources);
        return this;
    }
}

/// <summary>
/// Request model for updating a Thread
/// </summary>
public class UpdateThreadRequest : BaseRequest
{
    /// <summary>
    /// Creates a new UpdateThreadRequest
    /// </summary>
    public static UpdateThreadRequest Create()
    {
        return new UpdateThreadRequest();
    }

    /// <summary>
    /// Sets the metadata for the thread
    /// </summary>
    public UpdateThreadRequest WithMetadata(Dictionary<string, string> metadata)
    {
        SetValue(PropertyNames.Metadata, metadata);
        return this;
    }

    /// <summary>
    /// Sets the tool resources for the thread
    /// </summary>
    public UpdateThreadRequest WithToolResources(object toolResources)
    {
        SetValue(PropertyNames.ToolResources, toolResources);
        return this;
    }
}