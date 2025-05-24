namespace LMSupplyDepots.ModelHub.Utils;

/// <summary>
/// Represents model directory structure information
/// </summary>
public class ModelFileStructure
{
    /// <summary>
    /// Gets the model ID
    /// </summary>
    public string ModelId { get; init; } = string.Empty;

    /// <summary>
    /// Gets the registry information
    /// </summary>
    public string Registry { get; init; } = string.Empty;

    /// <summary>
    /// Gets the publisher name
    /// </summary>
    public string Publisher { get; init; } = string.Empty;

    /// <summary>
    /// Gets the model name
    /// </summary>
    public string ModelName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the artifact name
    /// </summary>
    public string ArtifactName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the model type
    /// </summary>
    public ModelType ModelType { get; init; }

    /// <summary>
    /// Gets the model format
    /// </summary>
    public string Format { get; init; } = string.Empty;

    /// <summary>
    /// Gets the base directory path
    /// </summary>
    public string BasePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the models directory path
    /// </summary>
    public string ModelsPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the path to the model type directory
    /// </summary>
    public string ModelTypePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the path to the publisher directory
    /// </summary>
    public string PublisherPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the path to the model name directory
    /// </summary>
    public string ModelNamePath { get; init; } = string.Empty;
}