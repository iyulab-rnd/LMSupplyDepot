namespace LMSupplyDepot.Tools.OpenAI.Models;

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