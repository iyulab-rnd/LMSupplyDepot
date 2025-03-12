namespace LMSupplyDepot.Tools.OpenAI.Models;

/// <summary>
/// Constants for truncation strategy types
/// </summary>
public static class TruncationStrategyTypes
{
    /// <summary>
    /// Automatic truncation strategy
    /// </summary>
    public const string Auto = "auto";

    /// <summary>
    /// Truncation strategy that only keeps the most recent messages
    /// </summary>
    public const string Last = "last";

    /// <summary>
    /// Truncation strategy that only keeps the first messages
    /// </summary>
    public const string First = "first";

    /// <summary>
    /// Truncation strategy that keeps both first and last messages
    /// </summary>
    public const string FirstAndLast = "first_and_last";
}

/// <summary>
/// Represents a truncation strategy for assistant runs
/// </summary>
public class TruncationStrategy : BaseModel
{
    /// <summary>
    /// The type of truncation strategy
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// Creates an auto truncation strategy
    /// </summary>
    public static TruncationStrategy CreateAuto()
    {
        return new TruncationStrategy { Type = TruncationStrategyTypes.Auto };
    }

    /// <summary>
    /// Creates a truncation strategy that only keeps the most recent messages
    /// </summary>
    public static TruncationStrategy CreateLast()
    {
        return new TruncationStrategy { Type = TruncationStrategyTypes.Last };
    }

    /// <summary>
    /// Creates a truncation strategy that only keeps the first messages
    /// </summary>
    public static TruncationStrategy CreateFirst()
    {
        return new TruncationStrategy { Type = TruncationStrategyTypes.First };
    }

    /// <summary>
    /// Creates a truncation strategy that keeps both first and last messages
    /// </summary>
    public static TruncationStrategy CreateFirstAndLast(int firstMessages, int lastMessages)
    {
        var strategy = new TruncationStrategy { Type = TruncationStrategyTypes.FirstAndLast };

        var config = new Dictionary<string, int>
        {
            { "first_messages", firstMessages },
            { "last_messages", lastMessages }
        };

        strategy.SetValue("parameters", config);
        return strategy;
    }

    /// <summary>
    /// Gets the number of first messages to keep if this is a first_and_last strategy
    /// </summary>
    public int? GetFirstMessages()
    {
        if (Type != TruncationStrategyTypes.FirstAndLast)
            return null;

        var parameters = GetValue<Dictionary<string, JsonElement>>("parameters");
        if (parameters == null || !parameters.ContainsKey("first_messages"))
            return null;

        return parameters["first_messages"].GetInt32();
    }

    /// <summary>
    /// Gets the number of last messages to keep if this is a first_and_last strategy
    /// </summary>
    public int? GetLastMessages()
    {
        if (Type != TruncationStrategyTypes.FirstAndLast)
            return null;

        var parameters = GetValue<Dictionary<string, JsonElement>>("parameters");
        if (parameters == null || !parameters.ContainsKey("last_messages"))
            return null;

        return parameters["last_messages"].GetInt32();
    }
}