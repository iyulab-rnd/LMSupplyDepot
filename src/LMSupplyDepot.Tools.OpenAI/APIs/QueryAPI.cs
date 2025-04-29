using OpenAI;
using OpenAI.Responses;

namespace LMSupplyDepot.Tools.OpenAI.APIs;

/// <summary>
/// Manages query operations with OpenAI for retrieval augmented generation
/// </summary>
public class QueryAPI
{
    private readonly OpenAIResponseClient _responseClient;

    /// <summary>
    /// Initializes a new instance of the QueryManager class
    /// </summary>
    /// <param name="client">The OpenAI client</param>
    /// <param name="model">The OpenAI model to use for responses</param>
    public QueryAPI(OpenAIClient client, string model = "gpt-4o-mini")
    {
        _responseClient = client.GetOpenAIResponseClient(model);
    }

    /// <summary>
    /// Queries the content of files using the Responses API with file search
    /// </summary>
    /// <param name="vectorStoreId">ID of the vector store to search</param>
    /// <param name="query">The user query to process</param>
    /// <returns>The response from OpenAI</returns>
    public async Task<OpenAIResponse> QueryFilesAsync(string vectorStoreId, string query)
    {
        Console.WriteLine($"Querying files with: \"{query}\"");

        try
        {
            var fileSearchTool = ResponseTool.CreateFileSearchTool(vectorStoreIds: [vectorStoreId]);

            var response = await _responseClient.CreateResponseAsync(
                userInputText: query,
                new ResponseCreationOptions
                {
                    Tools = { fileSearchTool }
                });

            return response;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error querying files: {ex.Message}");
            throw;
        }
    }
}