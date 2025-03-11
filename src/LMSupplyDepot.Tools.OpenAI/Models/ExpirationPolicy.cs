namespace LMSupplyDepot.Tools.OpenAI.Models;

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