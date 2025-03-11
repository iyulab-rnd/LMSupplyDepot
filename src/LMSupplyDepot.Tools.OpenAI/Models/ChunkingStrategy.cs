namespace LMSupplyDepot.Tools.OpenAI.Models;

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