namespace LMSupplyDepots.ModelHub.Models;

/// <summary>
/// Represents a model repository containing multiple artifacts
/// </summary>
public class LMRepo
{
    /// <summary>
    /// Repository identifier
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Model registry (e.g., "hf", "local")
    /// </summary>
    public string Registry { get; set; } = string.Empty;

    /// <summary>
    /// Repository ID of the model
    /// </summary>
    public string RepoId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the repository
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The type of models in this repository
    /// </summary>
    public ModelType Type { get; set; }

    /// <summary>
    /// Default format of models in this repository
    /// </summary>
    public string DefaultFormat { get; set; } = string.Empty;

    /// <summary>
    /// Version information for the repository
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// Brief description of the repository
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Publisher or owner of the repository
    /// </summary>
    public string Publisher { get; set; } = string.Empty;

    /// <summary>
    /// Available artifacts in this model repository
    /// </summary>
    public List<ModelArtifact> AvailableArtifacts { get; set; } = new();

    /// <summary>
    /// Common capabilities for all models in this repository
    /// </summary>
    public LMModelCapabilities Capabilities { get; set; } = new();

    /// <summary>
    /// Get a specific model from this repository by artifact name
    /// </summary>
    public LMModel? GetModel(string artifactName)
    {
        var artifact = AvailableArtifacts.FirstOrDefault(a => a.Name == artifactName);
        if (artifact == null)
            return null;

        // Use ModelFactory to create model from repository and artifact
        return Utils.ModelFactory.FromRepositoryAndArtifact(this, artifact);
    }

    /// <summary>
    /// Get all models from this repository
    /// </summary>
    public IReadOnlyList<LMModel> GetAllModels()
    {
        return AvailableArtifacts
            .Select(a => Utils.ModelFactory.FromRepositoryAndArtifact(this, a))
            .ToList();
    }

    /// <summary>
    /// Gets the recommended model from this repository
    /// </summary>
    public LMModel? GetRecommendedModel()
    {
        if (AvailableArtifacts.Count == 0)
            return null;

        // Try to find a medium-sized model first (good balance between size and quality)
        var mediumSizedArtifact = AvailableArtifacts.FirstOrDefault(a =>
            a.SizeCategory == "M" ||
            a.Name.Contains("Q5_K_M") ||
            a.Name.Contains("Q4_K_M") ||
            a.Name.Contains("_M") ||
            a.Name.Contains("-medium"));

        if (mediumSizedArtifact != null)
            return Utils.ModelFactory.FromRepositoryAndArtifact(this, mediumSizedArtifact);

        // Look for Q5 models which provide good quality/size balance
        var q5Artifact = AvailableArtifacts.FirstOrDefault(a =>
            a.QuantizationBits == 5 ||
            a.Name.Contains("Q5"));

        if (q5Artifact != null)
            return Utils.ModelFactory.FromRepositoryAndArtifact(this, q5Artifact);

        // Look for Q4 models which provide decent quality/size balance
        var q4Artifact = AvailableArtifacts.FirstOrDefault(a =>
            a.QuantizationBits == 4 ||
            a.Name.Contains("Q4"));

        if (q4Artifact != null)
            return Utils.ModelFactory.FromRepositoryAndArtifact(this, q4Artifact);

        // If no specific preference found, just use the first available artifact
        return Utils.ModelFactory.FromRepositoryAndArtifact(this, AvailableArtifacts[0]);
    }

    /// <summary>
    /// Creates a copy of this repository
    /// </summary>
    public LMRepo Clone()
    {
        return new LMRepo
        {
            Id = Id,
            Registry = Registry,
            RepoId = RepoId,
            Name = Name,
            Type = Type,
            DefaultFormat = DefaultFormat,
            Version = Version,
            Description = Description,
            Publisher = Publisher,
            Capabilities = Capabilities.Clone(),
            AvailableArtifacts = AvailableArtifacts.Select(a => a.Clone()).ToList()
        };
    }
}