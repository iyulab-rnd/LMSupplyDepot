using System.Diagnostics;

namespace LMSupplyDepots.ModelHub.Models;

/// <summary>
/// Value object representing a model identifier with immutable properties
/// </summary>
public readonly struct ModelIdentifier : IEquatable<ModelIdentifier>
{
    /// <summary>
    /// Registry of the model (e.g., "huggingface", "local")
    /// </summary>
    public string Registry { get; }

    /// <summary>
    /// Publisher of the model (e.g., "meta", "mistral")
    /// </summary>
    public string Publisher { get; }

    /// <summary>
    /// Name of the model (e.g., "Llama-3-8B-Instruct")
    /// </summary>
    public string ModelName { get; }

    /// <summary>
    /// Specific artifact name (e.g., "Llama-3-8B-Instruct-Q4_K_M")
    /// </summary>
    public string ArtifactName { get; }

    /// <summary>
    /// File format of the model (e.g., "gguf", "safetensors")
    /// </summary>
    public string Format { get; }

    /// <summary>
    /// Model type (TextGeneration, Embedding, etc.)
    /// </summary>
    public ModelType ModelType { get; }

    /// <summary>
    /// For backwards compatibility
    /// </summary>
    public string RepoId => $"{Publisher}/{ModelName}";

    /// <summary>
    /// Creates a new immutable model identifier
    /// </summary>
    public ModelIdentifier(
        string registry,
        string publisher,
        string modelName,
        string artifactName,
        string format = "gguf",
        ModelType modelType = ModelType.TextGeneration)
    {
        Registry = registry ?? throw new ArgumentNullException(nameof(registry));
        Publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        ModelName = modelName ?? throw new ArgumentNullException(nameof(modelName));
        ArtifactName = artifactName ?? throw new ArgumentNullException(nameof(artifactName));
        Format = format ?? "gguf";
        ModelType = modelType;
    }

    /// <summary>
    /// Parses a model ID string into a ModelIdentifier
    /// </summary>
    public static ModelIdentifier Parse(string modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
        {
            throw new ArgumentException("Model ID cannot be empty", nameof(modelId));
        }

        // Handle full format: registry:publisher/modelName/artifactName
        var registrySplit = modelId.Split(new[] { ':' }, 2);

        string registry;
        string remaining;

        if (registrySplit.Length == 2)
        {
            registry = registrySplit[0];
            remaining = registrySplit[1];
        }
        else
        {
            registry = "local";
            remaining = modelId;
        }

        // Handle the rest of the path parts
        var pathParts = remaining.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

        string publisher;
        string modelName;
        string artifactName;
        string format = "gguf";
        var modelType = ModelType.TextGeneration;

        if (pathParts.Length == 1)
        {
            // Just one part - treat it as both publisher and model name
            publisher = "local";
            modelName = pathParts[0];
            artifactName = Path.GetFileNameWithoutExtension(modelName);
        }
        else if (pathParts.Length == 2)
        {
            // publisher/model - a standard format
            publisher = pathParts[0];
            modelName = pathParts[1];
            artifactName = modelName;
        }
        else if (pathParts.Length >= 3)
        {
            // publisher/model/artifact - a full format
            publisher = pathParts[0];
            modelName = pathParts[1];
            artifactName = pathParts[2];

            // Check if we have a type indicator in the artifact name
            if (artifactName.Contains("embed", StringComparison.OrdinalIgnoreCase))
            {
                modelType = ModelType.Embedding;
            }
        }
        else
        {
            throw new ArgumentException($"Invalid model ID format: {modelId}", nameof(modelId));
        }

        // Try to detect format from extension in artifact name
        if (artifactName.Contains('.'))
        {
            var extension = Path.GetExtension(artifactName);
            if (!string.IsNullOrEmpty(extension))
            {
                format = extension.TrimStart('.').ToLowerInvariant();
                // Remove extension from artifactName
                artifactName = Path.GetFileNameWithoutExtension(artifactName);
            }
        }

        // Default to not multi-file - this will be determined elsewhere based on actual files
        return new ModelIdentifier(registry, publisher, modelName, artifactName, format, modelType);
    }

    /// <summary>
    /// Tries to parse a model ID string into a ModelIdentifier
    /// </summary>
    public static bool TryParse(string modelId, out ModelIdentifier identifier)
    {
        try
        {
            identifier = Parse(modelId);
            return true;
        }
        catch
        {
            identifier = default;
            return false;
        }
    }

    /// <summary>
    /// Creates a local model identifier
    /// </summary>
    public static ModelIdentifier CreateLocal(string name, ModelType modelType = ModelType.TextGeneration, string format = "gguf")
    {
        return new ModelIdentifier("local", "local", name, name, format, modelType);
    }

    /// <summary>
    /// Creates a HuggingFace model identifier
    /// </summary>
    public static ModelIdentifier CreateHuggingFace(
        string publisher,
        string modelName,
        string artifactName,
        ModelType modelType = ModelType.TextGeneration,
        string format = "gguf")
    {
        return new ModelIdentifier("hf", publisher, modelName, artifactName, format, modelType);
    }

    /// <summary>
    /// Converts an LMModel to a ModelIdentifier
    /// </summary>
    public static ModelIdentifier FromLMModel(LMModel model)
    {
        if (model == null)
            throw new ArgumentNullException(nameof(model));

        // Handle missing or incomplete fields by using defaults or inferring from other fields
        string registry = !string.IsNullOrEmpty(model.Registry) ? model.Registry : "local";

        string publisher;
        string modelName;

        // Parse RepoId into publisher and modelName
        if (!string.IsNullOrEmpty(model.RepoId) && model.RepoId.Contains('/'))
        {
            var repoParts = model.RepoId.Split('/');
            publisher = repoParts[0];
            modelName = string.Join("/", repoParts.Skip(1));
        }
        else
        {
            publisher = "local";
            modelName = !string.IsNullOrEmpty(model.RepoId) ? model.RepoId : model.Name;
        }

        string artifactName = !string.IsNullOrEmpty(model.ArtifactName) ? model.ArtifactName : model.Name;
        string format = !string.IsNullOrEmpty(model.Format) ? model.Format : "gguf";
        var modelType = model.Type;

        return new ModelIdentifier(registry, publisher, modelName, artifactName, format, modelType);
    }

    /// <summary>
    /// Updates an LMModel with information from this ModelIdentifier
    /// </summary>
    public void UpdateLMModel(LMModel model)
    {
        model.Id = ToString();
        model.Registry = Registry;
        model.RepoId = $"{Publisher}/{ModelName}";
        model.ArtifactName = ArtifactName;
        model.Format = Format;
        model.Type = ModelType;
    }

    /// <summary>
    /// Returns the fully qualified ID of this model
    /// </summary>
    public override string ToString()
    {
        return $"{Registry}:{Publisher}/{ModelName}/{ArtifactName}";
    }

    /// <summary>
    /// Creates a new ModelIdentifier with updated artifact name
    /// </summary>
    public ModelIdentifier WithArtifactName(string newArtifactName)
    {
        return new ModelIdentifier(Registry, Publisher, ModelName, newArtifactName, Format, ModelType);
    }

    /// <summary>
    /// Creates a new ModelIdentifier with updated format
    /// </summary>
    public ModelIdentifier WithFormat(string newFormat)
    {
        return new ModelIdentifier(Registry, Publisher, ModelName, ArtifactName, newFormat, ModelType);
    }

    /// <summary>
    /// Creates a new ModelIdentifier with updated model type
    /// </summary>
    public ModelIdentifier WithModelType(ModelType modelType)
    {
        return new ModelIdentifier(Registry, Publisher, ModelName, ArtifactName, Format, modelType);
    }

    /// <summary>
    /// Determines if this ModelIdentifier is equal to another object
    /// </summary>
    public override bool Equals(object? obj)
    {
        return obj is ModelIdentifier identifier && Equals(identifier);
    }

    /// <summary>
    /// Determines if this ModelIdentifier is equal to another ModelIdentifier
    /// </summary>
    public bool Equals(ModelIdentifier other)
    {
        return Registry == other.Registry &&
               Publisher == other.Publisher &&
               ModelName == other.ModelName &&
               ArtifactName == other.ArtifactName &&
               Format == other.Format &&
               ModelType == other.ModelType;
    }

    /// <summary>
    /// Gets the hash code for this ModelIdentifier
    /// </summary>
    public override int GetHashCode()
    {
        return HashCode.Combine(Registry, Publisher, ModelName, ArtifactName, Format, ModelType);
    }

    /// <summary>
    /// Equality operator
    /// </summary>
    public static bool operator ==(ModelIdentifier left, ModelIdentifier right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Inequality operator
    /// </summary>
    public static bool operator !=(ModelIdentifier left, ModelIdentifier right)
    {
        return !(left == right);
    }
}