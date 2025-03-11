namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Client for interacting with the OpenAI Assistants API
/// </summary>
public partial class OpenAIAssistantsClient : OpenAIBaseClient
{
    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAIAssistantsClient"/> class
    /// </summary>
    public OpenAIAssistantsClient(string apiKey, HttpClient? httpClient = null)
        : base(apiKey, httpClient)
    {
        SetupAssistantsApiHeader();
    }
}