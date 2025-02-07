using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace LMSupplyDepots.Tools.HuggingFace.Models;

/// <summary>
/// Represents a HuggingFace model.
/// </summary>
public partial class HuggingFaceModel
{
    /// <summary>
    /// the Repository ID
    /// </summary>
    [JsonPropertyName("id")]
    public string ID { get; set; } = string.Empty;

    /// <summary>
    /// the model ID.
    /// </summary>
    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>
    /// the author of the model.
    /// </summary>
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    /// <summary>
    /// the number of likes for the model.
    /// </summary>
    [JsonPropertyName("likes")]
    public long Likes { get; set; }

    /// <summary>
    /// the number of downloads for the model.
    /// </summary>
    [JsonPropertyName("downloads")]
    public long Downloads { get; set; }

    /// <summary>
    /// the creation date of the model.
    /// </summary>
    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// the last modified date of the model.
    /// </summary>
    [JsonPropertyName("lastModified")]
    public DateTime LastModified { get; set; }

    /// <summary>
    /// Represents the collection of files associated with the model,
    /// such as configuration files, tokenizer files, and other necessary components
    /// that are stored in the HuggingFace repository.
    /// </summary>
    [JsonPropertyName("siblings")]
    public ModelResource[] Siblings { get; set; } = [];

    /// <summary>
    /// Get the file paths for the model with the specified pattern.
    /// </summary>
    /// <param name="pattern">The regular expression pattern to match file path.</param>
    /// <returns>An array of file paths.</returns>
    public string[] GetFilePaths(Regex? pattern = null)
    {
        var files = Siblings
            .Where(sibling => !string.IsNullOrWhiteSpace(sibling.Rfilename))
            .Select(sibling => sibling.Rfilename)
            .ToArray();

        return pattern is null
            ? files
            : files.Where(file => pattern.IsMatch(file)).ToArray();
    }
}

public class ModelResource
{
    /// <summary>
    /// the file path in the repository.
    /// </summary>
    [JsonPropertyName("rfilename")]
    public string Rfilename { get; set; } = string.Empty;
}
