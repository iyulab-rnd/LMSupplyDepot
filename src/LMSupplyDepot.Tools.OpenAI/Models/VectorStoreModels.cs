namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Constants for vector store status values
/// </summary>
public static class VectorStoreStatus
{
    public const string Completed = "completed";
    public const string InProgress = "in_progress";
    public const string Expired = "expired";
}

/// <summary>
/// Constants for vector store file status values
/// </summary>
public static class VectorStoreFileStatus
{
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string Failed = "failed";
    public const string Cancelled = "cancelled";
}

/// <summary>
/// Constants for chunking strategy types
/// </summary>
public static class ChunkingStrategyTypes
{
    public const string Auto = "auto";
    public const string Static = "static";
}

/// <summary>
/// Represents a chunking strategy for file processing
/// </summary>
public class ChunkingStrategy : BaseModel
{
    /// <summary>
    /// The type of chunking strategy
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Creates an auto chunking strategy
    /// </summary>
    public static ChunkingStrategy CreateAutoStrategy()
    {
        return new ChunkingStrategy { Type = ChunkingStrategyTypes.Auto };
    }

    /// <summary>
    /// Creates a static chunking strategy
    /// </summary>
    public static ChunkingStrategy CreateStaticStrategy(int maxChunkSizeTokens, int chunkOverlapTokens)
    {
        var strategy = new ChunkingStrategy { Type = ChunkingStrategyTypes.Static };

        var staticConfig = new Dictionary<string, int>
        {
            { "max_chunk_size_tokens", maxChunkSizeTokens },
            { "chunk_overlap_tokens", chunkOverlapTokens }
        };

        strategy.SetValue("static", staticConfig);
        return strategy;
    }

    /// <summary>
    /// Gets the maximum chunk size in tokens if this is a static strategy
    /// </summary>
    public int? GetMaxChunkSizeTokens()
    {
        if (Type != ChunkingStrategyTypes.Static)
            return null;

        var staticConfig = GetValue<Dictionary<string, JsonElement>>("static");
        if (staticConfig == null || !staticConfig.ContainsKey("max_chunk_size_tokens"))
            return null;

        return staticConfig["max_chunk_size_tokens"].GetInt32();
    }

    /// <summary>
    /// Gets the chunk overlap in tokens if this is a static strategy
    /// </summary>
    public int? GetChunkOverlapTokens()
    {
        if (Type != ChunkingStrategyTypes.Static)
            return null;

        var staticConfig = GetValue<Dictionary<string, JsonElement>>("static");
        if (staticConfig == null || !staticConfig.ContainsKey("chunk_overlap_tokens"))
            return null;

        return staticConfig["chunk_overlap_tokens"].GetInt32();
    }
}

/// <summary>
/// Represents an expiration policy for vector stores
/// </summary>
public class ExpirationPolicy : BaseModel
{
    /// <summary>
    /// The anchor timestamp after which the expiration policy applies
    /// </summary>
    [JsonPropertyName("anchor")]
    public string Anchor { get; set; }

    /// <summary>
    /// The number of days after the anchor time that the vector store will expire
    /// </summary>
    [JsonPropertyName("days")]
    public int Days { get; set; }

    /// <summary>
    /// Creates an expiration policy based on the last active time
    /// </summary>
    public static ExpirationPolicy CreateLastActivePolicy(int days)
    {
        return new ExpirationPolicy
        {
            Anchor = "last_active_at",
            Days = days
        };
    }
}

/// <summary>
/// Represents file counts in a vector store
/// </summary>
public class VectorStoreFileCounts : BaseModel
{
    /// <summary>
    /// The number of files that are currently being processed
    /// </summary>
    [JsonPropertyName("in_progress")]
    public int InProgress { get; set; }

    /// <summary>
    /// The number of files that have been successfully processed
    /// </summary>
    [JsonPropertyName("completed")]
    public int Completed { get; set; }

    /// <summary>
    /// The number of files that have failed to process
    /// </summary>
    [JsonPropertyName("failed")]
    public int Failed { get; set; }

    /// <summary>
    /// The number of files that were cancelled
    /// </summary>
    [JsonPropertyName("cancelled")]
    public int Cancelled { get; set; }

    /// <summary>
    /// The total number of files
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }
}

/// <summary>
/// Represents a vector store file error
/// </summary>
public class VectorStoreFileError : BaseModel
{
    /// <summary>
    /// The error code
    /// </summary>
    [JsonPropertyName("code")]
    public string Code { get; set; }

    /// <summary>
    /// A human-readable description of the error
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; }
}

/// <summary>
/// Represents a vector store file
/// </summary>
public class VectorStoreFile : AssistantObject
{
    /// <summary>
    /// The total vector store usage in bytes
    /// </summary>
    [JsonPropertyName("usage_bytes")]
    public long? UsageBytes { get; set; }

    /// <summary>
    /// The ID of the vector store that the file is attached to
    /// </summary>
    [JsonPropertyName("vector_store_id")]
    public string VectorStoreId { get; set; }

    /// <summary>
    /// The status of the vector store file
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// The ID of the file in this vector store
    /// </summary>
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }

    /// <summary>
    /// Gets the last error associated with this vector store file
    /// </summary>
    public VectorStoreFileError GetLastError()
    {
        return GetValue<VectorStoreFileError>("last_error");
    }

    /// <summary>
    /// Gets the chunking strategy used for this file
    /// </summary>
    public ChunkingStrategy GetChunkingStrategy()
    {
        return GetValue<ChunkingStrategy>("chunking_strategy");
    }
}

/// <summary>
/// Represents a vector store file batch
/// </summary>
public class VectorStoreFileBatch : AssistantObject
{
    /// <summary>
    /// The ID of the vector store that the file batch belongs to
    /// </summary>
    [JsonPropertyName("vector_store_id")]
    public string VectorStoreId { get; set; }

    /// <summary>
    /// The status of the vector store file batch
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// The file counts in this batch
    /// </summary>
    [JsonPropertyName("file_counts")]
    public VectorStoreFileCounts FileCounts { get; set; }
}

/// <summary>
/// Ranking options configuration for file search tool
/// </summary>
public class FileSearchRankingOptions : BaseModel
{
    /// <summary>
    /// The type of ranker to use
    /// </summary>
    [JsonPropertyName("ranker")]
    public string Ranker { get; set; }

    /// <summary>
    /// The score threshold for including results
    /// </summary>
    [JsonPropertyName("score_threshold")]
    public double? ScoreThreshold { get; set; }

    /// <summary>
    /// Creates ranking options with the specified ranker and score threshold
    /// </summary>
    public static FileSearchRankingOptions Create(string ranker = "auto", double? scoreThreshold = null)
    {
        var options = new FileSearchRankingOptions { Ranker = ranker };

        if (scoreThreshold.HasValue)
        {
            options.ScoreThreshold = scoreThreshold.Value;
        }

        return options;
    }
}

/// <summary>
/// File search tool configuration
/// </summary>
public class FileSearchConfiguration : BaseModel
{
    /// <summary>
    /// Maximum number of search results to return
    /// </summary>
    [JsonPropertyName("max_num_results")]
    public int? MaxNumResults { get; set; }

    /// <summary>
    /// Gets the ranking options for the file search
    /// </summary>
    public FileSearchRankingOptions GetRankingOptions()
    {
        return GetValue<FileSearchRankingOptions>("ranking_options");
    }

    /// <summary>
    /// Sets the ranking options for the file search
    /// </summary>
    public FileSearchConfiguration WithRankingOptions(FileSearchRankingOptions rankingOptions)
    {
        SetValue("ranking_options", rankingOptions);
        return this;
    }

    /// <summary>
    /// Sets the maximum number of search results to return
    /// </summary>
    public FileSearchConfiguration WithMaxNumResults(int maxNumResults)
    {
        MaxNumResults = maxNumResults;
        return this;
    }

    /// <summary>
    /// Creates a new file search configuration
    /// </summary>
    public static FileSearchConfiguration Create(int? maxNumResults = null, string? ranker = null, double? scoreThreshold = null)
    {
        var config = new FileSearchConfiguration();

        if (maxNumResults.HasValue)
        {
            config.MaxNumResults = maxNumResults.Value;
        }

        if (!string.IsNullOrEmpty(ranker) || scoreThreshold.HasValue)
        {
            var rankingOptions = FileSearchRankingOptions.Create(ranker, scoreThreshold);
            config.WithRankingOptions(rankingOptions);
        }

        return config;
    }
}

/// <summary>
/// Represents file search result content in a run step
/// </summary>
public class FileSearchResultContent : BaseModel
{
    /// <summary>
    /// The content of the search result
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; }

    /// <summary>
    /// The file ID of the content
    /// </summary>
    [JsonPropertyName("file_id")]
    public string FileId { get; set; }

    /// <summary>
    /// Gets the score of the search result if available
    /// </summary>
    public double? GetScore()
    {
        return GetValue<double?>("score");
    }
}

/// <summary>
/// Extensions for creating file search tool with the FileSearchConfiguration
/// </summary>
public static class FileSearchToolExtensions
{
    /// <summary>
    /// Creates a file search tool with the FileSearchConfiguration
    /// </summary>
    public static Tool CreateFileSearchTool(int? maxNumResults = null, string? ranker = null, double? scoreThreshold = null)
    {
        var tool = new Tool { Type = ToolTypes.FileSearch };

        if (maxNumResults.HasValue || !string.IsNullOrEmpty(ranker) || scoreThreshold.HasValue)
        {
            var fileSearchConfig = FileSearchConfiguration.Create(maxNumResults, ranker, scoreThreshold);
            tool.SetValue("file_search", fileSearchConfig);
        }

        return tool;
    }
}

/// <summary>
/// Extensions for CreateVectorStoreFileRequest
/// </summary>
public static class VectorStoreFileRequestExtensions
{
    /// <summary>
    /// Sets the chunking strategy for the file
    /// </summary>
    public static CreateVectorStoreFileRequest WithChunkingStrategy(this CreateVectorStoreFileRequest request, ChunkingStrategy chunkingStrategy)
    {
        request.SetValue("chunking_strategy", chunkingStrategy);
        return request;
    }

    /// <summary>
    /// Sets a static chunking strategy for the file
    /// </summary>
    public static CreateVectorStoreFileRequest WithStaticChunkingStrategy(this CreateVectorStoreFileRequest request, int maxChunkSizeTokens, int chunkOverlapTokens)
    {
        var strategy = ChunkingStrategy.CreateStaticStrategy(maxChunkSizeTokens, chunkOverlapTokens);
        return request.WithChunkingStrategy(strategy);
    }

    /// <summary>
    /// Sets an auto chunking strategy for the file
    /// </summary>
    public static CreateVectorStoreFileRequest WithAutoChunkingStrategy(this CreateVectorStoreFileRequest request)
    {
        var strategy = ChunkingStrategy.CreateAutoStrategy();
        return request.WithChunkingStrategy(strategy);
    }
}

/// <summary>
/// Extensions for CreateVectorStoreFileBatchRequest
/// </summary>
public static class VectorStoreFileBatchRequestExtensions
{
    /// <summary>
    /// Sets the chunking strategy for the files
    /// </summary>
    public static CreateVectorStoreFileBatchRequest WithChunkingStrategy(this CreateVectorStoreFileBatchRequest request, ChunkingStrategy chunkingStrategy)
    {
        request.SetValue("chunking_strategy", chunkingStrategy);
        return request;
    }

    /// <summary>
    /// Sets a static chunking strategy for the files
    /// </summary>
    public static CreateVectorStoreFileBatchRequest WithStaticChunkingStrategy(this CreateVectorStoreFileBatchRequest request, int maxChunkSizeTokens, int chunkOverlapTokens)
    {
        var strategy = ChunkingStrategy.CreateStaticStrategy(maxChunkSizeTokens, chunkOverlapTokens);
        return request.WithChunkingStrategy(strategy);
    }

    /// <summary>
    /// Sets an auto chunking strategy for the files
    /// </summary>
    public static CreateVectorStoreFileBatchRequest WithAutoChunkingStrategy(this CreateVectorStoreFileBatchRequest request)
    {
        var strategy = ChunkingStrategy.CreateAutoStrategy();
        return request.WithChunkingStrategy(strategy);
    }
}

/// <summary>
/// Extensions for UpdateVectorStoreRequest
/// </summary>
public static class VectorStoreRequestExtensions
{
    /// <summary>
    /// Sets the expiration policy for the vector store
    /// </summary>
    public static UpdateVectorStoreRequest WithExpirationPolicy(this UpdateVectorStoreRequest request, ExpirationPolicy expirationPolicy)
    {
        request.SetValue("expires_after", expirationPolicy);
        return request;
    }

    /// <summary>
    /// Sets the expiration policy based on last active time
    /// </summary>
    public static UpdateVectorStoreRequest WithLastActiveExpirationPolicy(this UpdateVectorStoreRequest request, int days)
    {
        var policy = ExpirationPolicy.CreateLastActivePolicy(days);
        return request.WithExpirationPolicy(policy);
    }

    /// <summary>
    /// Sets the metadata for the vector store
    /// </summary>
    public static UpdateVectorStoreRequest WithMetadata(this UpdateVectorStoreRequest request, Dictionary<string, string> metadata)
    {
        request.SetValue("metadata", metadata);
        return request;
    }
}

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
    /// Sets the chunking strategy for the files
    /// </summary>
    public static CreateVectorStoreRequest WithChunkingStrategy(this CreateVectorStoreRequest request, ChunkingStrategy chunkingStrategy)
    {
        request.SetValue("chunking_strategy", chunkingStrategy);
        return request;
    }

    /// <summary>
    /// Sets the metadata for the vector store
    /// </summary>
    public static CreateVectorStoreRequest WithMetadata(this CreateVectorStoreRequest request, Dictionary<string, string> metadata)
    {
        request.SetValue("metadata", metadata);
        return request;
    }
}