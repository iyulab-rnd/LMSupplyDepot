using LMSupplyDepot.Tools.OpenAI.APIs;
using OpenAI;

namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Service that provides access to OpenAI APIs
/// </summary>
public class OpenAIService
{
    private readonly OpenAIClient _client;

    /// <summary>
    /// File operations API
    /// </summary>
    public FileAPI File { get; }

    /// <summary>
    /// Query operations API
    /// </summary>
    public QueryAPI Query { get; }

    /// <summary>
    /// Vector store operations API
    /// </summary>
    public VectorStoreAPI VectorStore { get; }

    /// <summary>
    /// Initializes a new instance of the OpenAIService class
    /// </summary>
    /// <param name="apiKey">OpenAI API key</param>
    /// <param name="model">Default model for queries (optional)</param>
    public OpenAIService(string apiKey, string model = "gpt-4o-mini")
    {
        _client = new OpenAIClient(apiKey);
        File = new FileAPI(_client);
        VectorStore = new VectorStoreAPI(_client);
        Query = new QueryAPI(_client, model);
    }

    /// <summary>
    /// Initializes a new instance of the OpenAIService class with an existing client
    /// </summary>
    /// <param name="client">OpenAI client instance</param>
    /// <param name="model">Default model for queries (optional)</param>
    public OpenAIService(OpenAIClient client, string model = "gpt-4o-mini")
    {
        _client = client;
        File = new FileAPI(_client);
        VectorStore = new VectorStoreAPI(_client);
        Query = new QueryAPI(_client, model);
    }
}