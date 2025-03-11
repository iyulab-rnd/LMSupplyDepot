namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Request model for creating a Vector Store
/// </summary>
public class CreateVectorStoreRequest : BaseRequest
{
    /// <summary>
    /// The name of the vector store
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// Creates a new CreateVectorStoreRequest with the specified name
    /// </summary>
    public static CreateVectorStoreRequest Create(string name)
    {
        return new CreateVectorStoreRequest { Name = name };
    }

    /// <summary>
    /// Sets the file IDs for the vector store
    /// </summary>
    public CreateVectorStoreRequest WithFileIds(List<string> fileIds)
    {
        SetValue("file_ids", fileIds);
        return this;
    }

    /// <summary>
    /// Sets the expiration policy for the vector store
    /// </summary>
    public CreateVectorStoreRequest WithExpiresAfter(object expiresAfter)
    {
        SetValue("expires_after", expiresAfter);
        return this;
    }

    /// <summary>
    /// Sets the chunking strategy for the files
    /// </summary>
    public CreateVectorStoreRequest WithChunkingStrategy(ChunkingStrategy chunkingStrategy)
    {
        SetValue("chunking_strategy", chunkingStrategy);
        return this;
    }

    /// <summary>
    /// Sets the metadata for the vector store
    /// </summary>
    public CreateVectorStoreRequest WithMetadata(Dictionary<string, string> metadata)
    {
        SetValue("metadata", metadata);
        return this;
    }
}

/// <summary>
/// Request model for updating a Vector Store
/// </summary>
public class UpdateVectorStoreRequest : BaseRequest
{
    /// <summary>
    /// Creates a new UpdateVectorStoreRequest
    /// </summary>
    public static UpdateVectorStoreRequest Create()
    {
        return new UpdateVectorStoreRequest();
    }

    /// <summary>
    /// Sets the name of the vector store
    /// </summary>
    public UpdateVectorStoreRequest WithName(string name)
    {
        SetValue("name", name);
        return this;
    }

    /// <summary>
    /// Sets the expiration policy for the vector store
    /// </summary>
    public UpdateVectorStoreRequest WithExpiresAfter(object expiresAfter)
    {
        SetValue("expires_after", expiresAfter);
        return this;
    }

    /// <summary>
    /// Sets the metadata for the vector store
    /// </summary>
    public UpdateVectorStoreRequest WithMetadata(Dictionary<string, string> metadata)
    {
        SetValue("metadata", metadata);
        return this;
    }
}

/// <summary>
/// Request model for creating a Vector Store file
/// </summary>
public class CreateVectorStoreFileRequest : BaseRequest
{
    /// <summary>
    /// The ID of the file to add to the vector store
    /// </summary>
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }

    /// <summary>
    /// Creates a new CreateVectorStoreFileRequest with the specified file ID
    /// </summary>
    public static CreateVectorStoreFileRequest Create(string fileId)
    {
        return new CreateVectorStoreFileRequest { FileId = fileId };
    }

    /// <summary>
    /// Sets the chunking strategy for the file
    /// </summary>
    public CreateVectorStoreFileRequest WithChunkingStrategy(ChunkingStrategy chunkingStrategy)
    {
        SetValue("chunking_strategy", chunkingStrategy);
        return this;
    }

    /// <summary>
    /// Sets a static chunking strategy for the file
    /// </summary>
    public CreateVectorStoreFileRequest WithStaticChunkingStrategy(int maxChunkSizeTokens, int chunkOverlapTokens)
    {
        var strategy = ChunkingStrategy.CreateStaticStrategy(maxChunkSizeTokens, chunkOverlapTokens);
        return WithChunkingStrategy(strategy);
    }

    /// <summary>
    /// Sets an auto chunking strategy for the file
    /// </summary>
    public CreateVectorStoreFileRequest WithAutoChunkingStrategy()
    {
        var strategy = ChunkingStrategy.CreateAutoStrategy();
        return WithChunkingStrategy(strategy);
    }
}

/// <summary>
/// Request model for creating a Vector Store file batch
/// </summary>
public class CreateVectorStoreFileBatchRequest : BaseRequest
{
    /// <summary>
    /// Creates a new CreateVectorStoreFileBatchRequest with the specified file IDs
    /// </summary>
    public static CreateVectorStoreFileBatchRequest Create(List<string> fileIds)
    {
        var request = new CreateVectorStoreFileBatchRequest();
        request.SetValue("file_ids", fileIds);
        return request;
    }

    /// <summary>
    /// Sets the chunking strategy for the files
    /// </summary>
    public CreateVectorStoreFileBatchRequest WithChunkingStrategy(ChunkingStrategy chunkingStrategy)
    {
        SetValue("chunking_strategy", chunkingStrategy);
        return this;
    }

    /// <summary>
    /// Sets a static chunking strategy for the files
    /// </summary>
    public CreateVectorStoreFileBatchRequest WithStaticChunkingStrategy(int maxChunkSizeTokens, int chunkOverlapTokens)
    {
        var strategy = ChunkingStrategy.CreateStaticStrategy(maxChunkSizeTokens, chunkOverlapTokens);
        return WithChunkingStrategy(strategy);
    }

    /// <summary>
    /// Sets an auto chunking strategy for the files
    /// </summary>
    public CreateVectorStoreFileBatchRequest WithAutoChunkingStrategy()
    {
        var strategy = ChunkingStrategy.CreateAutoStrategy();
        return WithChunkingStrategy(strategy);
    }
}