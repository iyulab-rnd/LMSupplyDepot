namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Extensions for CreateVectorStoreRequest
/// </summary>
public static class CreateVectorStoreRequestExtensions
{
    /// <summary>
    /// Sets the file IDs for initial population of the vector store
    /// </summary>
    public static CreateVectorStoreRequest WithFileIds(this CreateVectorStoreRequest request, List<string> fileIds)
    {
        request.SetValue("file_ids", fileIds);
        return request;
    }

    /// <summary>
    /// Sets the expiration policy for the vector store
    /// </summary>
    public static CreateVectorStoreRequest WithExpirationPolicy(this CreateVectorStoreRequest request, ExpirationPolicy expirationPolicy)
    {
        request.SetValue("expires_after", expirationPolicy);
        return request;
    }

    /// <summary>
    /// Sets the expiration policy based on last active time
    /// </summary>
    public static CreateVectorStoreRequest WithLastActiveExpirationPolicy(this CreateVectorStoreRequest request, int days)
    {
        var policy = ExpirationPolicy.CreateLastActivePolicy(days);
        return request.WithExpirationPolicy(policy);
    }

    /// <summary>
    /// Sets a static chunking strategy for the files
    /// </summary>
    public static CreateVectorStoreRequest WithStaticChunkingStrategy(this CreateVectorStoreRequest request, int maxChunkSizeTokens, int chunkOverlapTokens)
    {
        var strategy = ChunkingStrategy.CreateStaticStrategy(maxChunkSizeTokens, chunkOverlapTokens);
        return request.WithChunkingStrategy(strategy);
    }

    /// <summary>
    /// Sets an auto chunking strategy for the files
    /// </summary>
    public static CreateVectorStoreRequest WithAutoChunkingStrategy(this CreateVectorStoreRequest request)
    {
        var strategy = ChunkingStrategy.CreateAutoStrategy();
        return request.WithChunkingStrategy(strategy);
    }
}