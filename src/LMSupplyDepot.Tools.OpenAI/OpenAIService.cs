using LMSupplyDepot.Tools.OpenAI.APIs;
using OpenAI;

namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Service that provides access to OpenAI APIs
/// </summary>
public class OpenAIService
{
    /// <summary>
    /// File operations API
    /// </summary>
    public FileAPI File { get; private set; }

    /// <summary>
    /// Query operations API
    /// </summary>
    public QueryAPI Query { get; private set; }

    /// <summary>
    /// Chat operations API
    /// </summary>
    public ChatAPI Chat { get; private set; }

    /// <summary>
    /// Vector store operations API
    /// </summary>
    public VectorStoreAPI VectorStore { get; private set; }

    public OpenAIService(string apiKey, string queryModel = "gpt-4o-mini", string chatModel = "gpt-4o")
    {
        var client = new OpenAIClient(apiKey);

        File = new FileAPI(client);
        VectorStore = new VectorStoreAPI(client);
        Query = new QueryAPI(client, queryModel);
        Chat = new ChatAPI(client, chatModel);
    }

    public OpenAIService(OpenAIClient client, string queryModel = "gpt-4o-mini", string chatModel = "gpt-4o")
    {
        File = new FileAPI(client);
        VectorStore = new VectorStoreAPI(client);
        Query = new QueryAPI(client, queryModel);
        Chat = new ChatAPI(client, chatModel);
    }
}