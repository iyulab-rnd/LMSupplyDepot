namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Represents a Run Step in the Assistants API
/// </summary>
public class RunStep : OpenAIResource
{
    /// <summary>
    /// The ID of the run that this run step is a part of
    /// </summary>
    [JsonPropertyName("run_id")]
    public string RunId { get; set; }

    /// <summary>
    /// The ID of the assistant associated with the run
    /// </summary>
    [JsonPropertyName("assistant_id")]
    public string AssistantId { get; set; }

    /// <summary>
    /// The ID of the thread that was executed on as a part of this run step
    /// </summary>
    [JsonPropertyName("thread_id")]
    public string ThreadId { get; set; }

    /// <summary>
    /// The type of run step
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// The status of the run step
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// Get the step details
    /// </summary>
    public RunStepDetails GetStepDetails()
    {
        return GetValue<RunStepDetails>("step_details");
    }
}