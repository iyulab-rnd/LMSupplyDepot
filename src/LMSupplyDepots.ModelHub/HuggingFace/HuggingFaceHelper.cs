using System.Text.RegularExpressions;
using LMSupplyDepots.External.HuggingFace.Models;
using LMSupplyDepots.External.HuggingFace.Client;
using System.Text.Json;
using System.Diagnostics;

namespace LMSupplyDepots.ModelHub.HuggingFace;

/// <summary>
/// Helper class for HuggingFace specific operations
/// </summary>
public static class HuggingFaceHelper
{
    // Common regex patterns
    private static readonly Regex _sourceIdRegex = new(@"^(hf|huggingface):(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex _quantizationRegex = new(@"Q(\d+)(_[KM])?(_S|_M|_L|_XL)?", RegexOptions.Compiled);
    private static readonly Regex _sizeCategoryRegex = new(@"(_XS|_S|_M|_L|_XL|-xs|-small|-medium|-large|-xl)$", RegexOptions.Compiled);
    private static readonly Regex _multiFileRegex = new(@"(.*?)[.-](\d{5})-of-(\d{5})$", RegexOptions.Compiled);

    /// <summary>
    /// Normalizes a source ID to remove the HuggingFace prefix
    /// </summary>
    public static string NormalizeSourceId(string sourceId)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            return sourceId;

        var match = _sourceIdRegex.Match(sourceId);
        if (match.Success)
        {
            return match.Groups[2].Value;
        }

        return sourceId;
    }

    /// <summary>
    /// Splits a source ID into repository ID and artifact name
    /// </summary>
    public static (string repoId, string? artifactName) NormalizeAndSplitSourceId(string sourceId)
    {
        var match = _sourceIdRegex.Match(sourceId);
        string normalizedId = match.Success ? match.Groups[2].Value : sourceId;

        // Check if there's an artifact name specified
        string? artifactName = null;
        string repoId = normalizedId;

        var lastSlashIndex = normalizedId.LastIndexOf('/');
        if (lastSlashIndex >= 0)
        {
            // There's at least one slash
            var parts = normalizedId.Split('/');

            // Format is expected to be: owner/repo/artifact
            // We want to extract the artifact name and make repoId be owner/repo
            if (parts.Length >= 3)
            {
                artifactName = parts[parts.Length - 1];
                repoId = string.Join("/", parts.Take(parts.Length - 1));
            }
            // Handle formats like owner/artifact, which are ambiguous
            // In this case, we'll assume it's actually just a repo ID without an artifact
            // The caller needs to handle artifact detection separately if needed
            else if (parts.Length == 2)
            {
                // Don't split by default, treat the whole thing as repoId
                // Let the caller handle artifact detection separately
                repoId = normalizedId;
                artifactName = null;
            }
        }

        return (repoId, artifactName);
    }

    /// <summary>
    /// Estimates the total size of a model based on available information
    /// </summary>
    public static long? CalculateTotalSize(HuggingFaceModel hfModel)
    {
        // Estimate total size based on model parameters and artifact count
        if (hfModel.Siblings != null && hfModel.Siblings.Count > 0)
        {
            // For now, we can't directly get file sizes from the model object
            // We'll estimate based on relevant file count and model type
            var ggufFiles = hfModel.GetGgufModelPaths();

            if (ggufFiles.Length > 0)
            {
                // For GGUF models, estimate based on filenames
                long totalEstimate = 0;
                foreach (var file in ggufFiles)
                {
                    totalEstimate += EstimateArtifactSize(Path.GetFileNameWithoutExtension(file), "gguf");
                }
                return totalEstimate;
            }
        }

        // Default size estimate for unknown models
        return null;
    }

    /// <summary>
    /// Gets file information from the sibling collection
    /// </summary>
    public static long EstimateFileSize(HuggingFaceModel hfModel, string filePath)
    {
        // We can't directly get size from siblings (no Size property available)
        // So we'll estimate based on the filename
        string fileName = Path.GetFileName(filePath);
        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName).TrimStart('.');

        return EstimateArtifactSize(baseName, extension);
    }

    /// <summary>
    /// Calculate the amount of data already downloaded based on the files present in the target directory
    /// </summary>
    public static long CalculateDownloadedSize(string targetDirectory)
    {
        if (!Directory.Exists(targetDirectory))
        {
            return 0;
        }

        try
        {
            // Get all files in the directory and subdirectories
            var files = Directory.GetFiles(targetDirectory, "*.*", SearchOption.AllDirectories);

            // Sum up the size of all files except temporary ones
            return files
                .Where(f => Path.GetExtension(f) != ".download" &&
                           Path.GetExtension(f) != ".part" &&
                           Path.GetExtension(f) != ".tmp")
                .Sum(f => new FileInfo(f).Length);
        }
        catch (Exception)
        {
            return 0;
        }
    }

    /// <summary>
    /// Determines the model type based on its tags and capabilities
    /// </summary>
    public static ModelType DetermineModelType(HuggingFaceModel hfModel)
    {
        var tags = hfModel.Tags.Select(t => t.ToLowerInvariant()).ToList();

        if (External.HuggingFace.Common.ModelTagValidation.IsTextGenerationModel(hfModel))
        {
            return ModelType.TextGeneration;
        }

        if (External.HuggingFace.Common.ModelTagValidation.IsEmbeddingModel(hfModel))
        {
            return ModelType.Embedding;
        }

        if (tags.Any(t => t.Contains("vision") || t.Contains("image-to-text")))
        {
            Debug.WriteLine($"Model {hfModel.ModelId} is a vision model.");
        }

        // Default to text generation if we can't determine
        return ModelType.TextGeneration;
    }

    /// <summary>
    /// Gets the format of a model based on its files
    /// </summary>
    public static string GetModelFormat(HuggingFaceModel hfModel)
    {
        if (hfModel.HasGgufFiles())
        {
            return "GGUF";
        }

        var weightFiles = hfModel.GetModelWeightPaths();
        if (weightFiles.Any(f => f.EndsWith(".safetensors", StringComparison.OrdinalIgnoreCase)))
        {
            return "SafeTensors";
        }

        if (weightFiles.Any(f => f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase)))
        {
            return "HF Binary";
        }

        return "Unknown";
    }

    /// <summary>
    /// Extracts a description for the model from its metadata
    /// </summary>
    public static string GetModelDescription(HuggingFaceModel hfModel)
    {
        var description = string.Empty;

        // Try to extract from the model card metadata if available
        if (hfModel.HasProperty("modelCardData") &&
            hfModel.GetProperty<JsonElement>("modelCardData") is JsonElement modelCard &&
            modelCard.TryGetProperty("model-index", out var modelIndex) &&
            modelIndex.TryGetProperty("description", out var descElement))
        {
            description = descElement.GetString() ?? string.Empty;
        }

        // If no description found, build one from tags and other metadata
        if (string.IsNullOrWhiteSpace(description))
        {
            var tags = string.Join(", ", hfModel.Tags.Take(5));
            description = $"{hfModel.ModelId} - Tags: {tags} - Created by {hfModel.Author}";
        }

        return description;
    }

    /// <summary>
    /// Determines the maximum context length of a model based on its metadata or name
    /// </summary>
    public static int GetMaxContextLength(HuggingFaceModel hfModel)
    {
        // Try to extract from GGUF metadata
        if (hfModel.GGUF.Count > 0 &&
            hfModel.GGUF.TryGetValue("general.context_length", out var contextLengthValue))
        {
            if (contextLengthValue.ValueKind == JsonValueKind.Number)
            {
                return contextLengthValue.GetInt32();
            }
        }

        // Use some heuristics based on model name
        var modelName = hfModel.ModelId.ToLowerInvariant();

        if (modelName.Contains("7b") || modelName.Contains("1b") || modelName.Contains("3b"))
            return 4096;

        if (modelName.Contains("13b") || modelName.Contains("14b"))
            return 8192;

        if (modelName.Contains("30b") || modelName.Contains("33b") || modelName.Contains("34b"))
            return 8192;

        if (modelName.Contains("70b") || modelName.Contains("65b") || modelName.Contains("mixtral"))
            return 32768;

        // Default value
        return 4096;
    }

    /// <summary>
    /// Determines the embedding dimension for embedding models
    /// </summary>
    public static int? GetEmbeddingDimension(HuggingFaceModel hfModel)
    {
        if (External.HuggingFace.Common.ModelTagValidation.IsEmbeddingModel(hfModel))
        {
            // Look for dimension in model name
            var match = Regex.Match(hfModel.ModelId, @"(\d+)d");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int dimension))
            {
                return dimension;
            }

            // Some common embedding dimensions
            if (hfModel.ModelId.Contains("minilm"))
                return 384;

            if (hfModel.ModelId.Contains("mpnet"))
                return 768;

            if (hfModel.ModelId.Contains("e5"))
                return 1024;

            // Default for embedding models
            return 768;
        }

        return null;
    }

    /// <summary>
    /// Extracts artifact information from repository files
    /// </summary>
    public static List<ModelArtifact> ExtractArtifacts(List<string> files, string defaultFormat)
    {
        var artifacts = new List<ModelArtifact>();
        var fileGroups = new Dictionary<string, List<string>>();

        // Sort files by preferred formats
        var preferredFiles = files
            .Where(f => IsModelFileExtension(Path.GetExtension(f)))
            .ToList();

        // Process each file to identify artifacts
        foreach (var file in preferredFiles)
        {
            var extension = Path.GetExtension(file);
            if (string.IsNullOrEmpty(extension)) continue;

            var format = extension.TrimStart('.').ToLowerInvariant();
            var baseName = Path.GetFileNameWithoutExtension(file);

            // Skip non-model files
            if (!IsModelFileExtension(extension)) continue;

            // Check for multi-file patterns (e.g., model.00001-of-00003.gguf)
            var multiFileMatch = _multiFileRegex.Match(baseName);
            if (multiFileMatch.Success)
            {
                var artifactBaseName = multiFileMatch.Groups[1].Value;
                var partNumber = int.Parse(multiFileMatch.Groups[2].Value);
                var totalParts = int.Parse(multiFileMatch.Groups[3].Value);

                if (!fileGroups.TryGetValue(artifactBaseName, out var group))
                {
                    group = new List<string>();
                    fileGroups[artifactBaseName] = group;
                }

                group.Add(file);
                continue;
            }

            // Parse quantization and size information
            int? quantBits = null;
            string? sizeCategory = null;

            // Check for quantization information
            var quantMatch = _quantizationRegex.Match(baseName);
            if (quantMatch.Success)
            {
                quantBits = int.Parse(quantMatch.Groups[1].Value);
            }

            // Check for size category
            var sizeMatch = _sizeCategoryRegex.Match(baseName);
            if (sizeMatch.Success)
            {
                sizeCategory = GetNormalizedSizeCategory(sizeMatch.Groups[1].Value);
            }

            // Single file artifact
            artifacts.Add(new ModelArtifact
            {
                Name = baseName,
                Format = format,
                FilePaths = new List<string> { file },
                Description = GetArtifactDescription(baseName, format),
                QuantizationBits = quantBits,
                SizeCategory = sizeCategory,
                // Set approximate size - will be updated during download
                SizeInBytes = EstimateArtifactSize(baseName, format)
            });
        }

        // Process multi-file groups
        foreach (var group in fileGroups)
        {
            if (group.Value.Count > 0)
            {
                var firstFile = group.Value[0];
                var format = Path.GetExtension(firstFile).TrimStart('.').ToLowerInvariant();
                var baseName = group.Key;

                // Parse quantization and size information
                int? quantBits = null;
                string? sizeCategory = null;

                // Check for quantization information
                var quantMatch = _quantizationRegex.Match(baseName);
                if (quantMatch.Success)
                {
                    quantBits = int.Parse(quantMatch.Groups[1].Value);
                }

                // Check for size category
                var sizeMatch = _sizeCategoryRegex.Match(baseName);
                if (sizeMatch.Success)
                {
                    sizeCategory = GetNormalizedSizeCategory(sizeMatch.Groups[1].Value);
                }

                artifacts.Add(new ModelArtifact
                {
                    Name = baseName,
                    Format = format,
                    FilePaths = group.Value,
                    Description = GetArtifactDescription(baseName, format, true),
                    QuantizationBits = quantBits,
                    SizeCategory = sizeCategory,
                    // Set approximate size based on the number of parts
                    SizeInBytes = EstimateMultiFileArtifactSize(baseName, format, group.Value.Count)
                });
            }
        }

        return artifacts;
    }

    /// <summary>
    /// Converts a HuggingFaceModel to our LMModel format
    /// </summary>
    public static LMModel ConvertToLMModel(HuggingFaceModel hfModel)
    {
        var modelType = DetermineModelType(hfModel);
        var format = GetModelFormat(hfModel);

        var model = new LMModel
        {
            Id = $"hf:{hfModel.ModelId}",
            Registry = "hf",
            RepoId = hfModel.ModelId,
            Name = string.IsNullOrEmpty(hfModel.ModelId) ?
                "Unknown Model" : Path.GetFileName(hfModel.ModelId),
            Type = modelType,
            Format = format,
            Version = hfModel.LastModified.ToString("yyyyMMdd"),
            Description = GetModelDescription(hfModel),
            Capabilities = new LMModelCapabilities
            {
                SupportsTextGeneration = modelType == ModelType.TextGeneration,
                SupportsEmbeddings = modelType == ModelType.Embedding,
                SupportsImageUnderstanding = hfModel.Tags.Any(t =>
                    t.Contains("vision") || t.Contains("image") || t.Contains("multimodal")),
                MaxContextLength = GetMaxContextLength(hfModel),
                EmbeddingDimension = GetEmbeddingDimension(hfModel)
            }
        };

        // Set a default artifact name using the repo name
        model.ArtifactName = hfModel.ModelId.Split('/').Last();

        return model;
    }

    /// <summary>
    /// Generates a description for an artifact based on its name and format
    /// </summary>
    private static string GetArtifactDescription(string artifactName, string format, bool isMultiFile = false)
    {
        var parts = new List<string>();

        // Add format info
        parts.Add($"{format.ToUpperInvariant()} format");

        // Check for quantization information in the name
        if (artifactName.Contains("Q4") || artifactName.Contains("Q5") || artifactName.Contains("Q8"))
        {
            var quantMatch = _quantizationRegex.Match(artifactName);
            if (quantMatch.Success)
            {
                parts.Add($"Q{quantMatch.Groups[1].Value} quantization");
            }
        }

        // Add size information if present
        var sizeMatch = _sizeCategoryRegex.Match(artifactName);
        if (sizeMatch.Success)
        {
            var sizeCategory = GetNormalizedSizeCategory(sizeMatch.Groups[1].Value);
            parts.Add($"{sizeCategory} size");
        }

        // Add multi-file info if applicable
        if (isMultiFile)
        {
            parts.Add("Multiple files");
        }

        return string.Join(", ", parts);
    }

    /// <summary>
    /// Normalizes size category string to standard form
    /// </summary>
    private static string GetNormalizedSizeCategory(string sizeCategory)
    {
        var normalized = sizeCategory.ToUpperInvariant();

        if (normalized.Contains("XS") || normalized == "-XS")
            return "Extra Small";
        if (normalized.Contains("S") || normalized == "-SMALL")
            return "Small";
        if (normalized.Contains("M") || normalized == "-MEDIUM")
            return "Medium";
        if (normalized.Contains("L") && !normalized.Contains("XL") || normalized == "-LARGE")
            return "Large";
        if (normalized.Contains("XL") || normalized == "-XL")
            return "Extra Large";

        return "Unknown Size";
    }

    /// <summary>
    /// Estimates the size of an artifact based on its name and format
    /// </summary>
    public static long EstimateArtifactSize(string artifactName, string format)
    {
        // Base size can be adjusted for different quantization methods and model sizes
        // These are rough estimates based on typical file sizes

        // Check for model size indicators in the name
        long baseSize = 0;

        // Quantization patterns to adjust the size estimation
        bool hasQ2 = artifactName.Contains("Q2", StringComparison.OrdinalIgnoreCase) ||
                     artifactName.Contains("IQ2", StringComparison.OrdinalIgnoreCase);
        bool hasQ3 = artifactName.Contains("Q3", StringComparison.OrdinalIgnoreCase) ||
                     artifactName.Contains("IQ3", StringComparison.OrdinalIgnoreCase);
        bool hasQ4 = artifactName.Contains("Q4", StringComparison.OrdinalIgnoreCase) ||
                     artifactName.Contains("IQ4", StringComparison.OrdinalIgnoreCase);
        bool hasQ5 = artifactName.Contains("Q5", StringComparison.OrdinalIgnoreCase) ||
                     artifactName.Contains("IQ5", StringComparison.OrdinalIgnoreCase);
        bool hasQ6 = artifactName.Contains("Q6", StringComparison.OrdinalIgnoreCase) ||
                     artifactName.Contains("IQ6", StringComparison.OrdinalIgnoreCase);
        bool hasQ8 = artifactName.Contains("Q8", StringComparison.OrdinalIgnoreCase) ||
                     artifactName.Contains("IQ8", StringComparison.OrdinalIgnoreCase);

        // Size indicators
        bool isXS = artifactName.Contains("_XS", StringComparison.OrdinalIgnoreCase) ||
                    artifactName.Contains("-XS", StringComparison.OrdinalIgnoreCase);
        bool isS = artifactName.Contains("_S", StringComparison.OrdinalIgnoreCase) && !isXS ||
                   artifactName.Contains("-S", StringComparison.OrdinalIgnoreCase) && !isXS;
        bool isM = artifactName.Contains("_M", StringComparison.OrdinalIgnoreCase) ||
                   artifactName.Contains("-M", StringComparison.OrdinalIgnoreCase);
        bool isL = artifactName.Contains("_L", StringComparison.OrdinalIgnoreCase) && !artifactName.Contains("_XL", StringComparison.OrdinalIgnoreCase) ||
                   artifactName.Contains("-L", StringComparison.OrdinalIgnoreCase) && !artifactName.Contains("-XL", StringComparison.OrdinalIgnoreCase);
        bool isXL = artifactName.Contains("_XL", StringComparison.OrdinalIgnoreCase) ||
                    artifactName.Contains("-XL", StringComparison.OrdinalIgnoreCase);

        // Model parameter size indicators
        if (artifactName.Contains("3.2-3B", StringComparison.OrdinalIgnoreCase) ||
            artifactName.Contains("3-3B", StringComparison.OrdinalIgnoreCase) ||
            artifactName.Contains("3B", StringComparison.OrdinalIgnoreCase))
        {
            // 3B models - based on observed sizes
            if (hasQ2) return isXS ? 250L * 1024 * 1024 : isS ? 300L * 1024 * 1024 : isM ? 350L * 1024 * 1024 : isL ? 400L * 1024 * 1024 : isXL ? 450L * 1024 * 1024 : 350L * 1024 * 1024;
            if (hasQ3) return isXS ? 300L * 1024 * 1024 : isS ? 350L * 1024 * 1024 : isM ? 400L * 1024 * 1024 : isL ? 450L * 1024 * 1024 : isXL ? 500L * 1024 * 1024 : 400L * 1024 * 1024;
            if (hasQ4) return isXS ? 350L * 1024 * 1024 : isS ? 400L * 1024 * 1024 : isM ? 450L * 1024 * 1024 : isL ? 500L * 1024 * 1024 : isXL ? 550L * 1024 * 1024 : 450L * 1024 * 1024;
            if (hasQ5) return isXS ? 400L * 1024 * 1024 : isS ? 450L * 1024 * 1024 : isM ? 500L * 1024 * 1024 : isL ? 550L * 1024 * 1024 : isXL ? 600L * 1024 * 1024 : 500L * 1024 * 1024;
            if (hasQ6) return isXS ? 450L * 1024 * 1024 : isS ? 500L * 1024 * 1024 : isM ? 550L * 1024 * 1024 : isL ? 600L * 1024 * 1024 : isXL ? 650L * 1024 * 1024 : 550L * 1024 * 1024;
            if (hasQ8) return isXS ? 500L * 1024 * 1024 : isS ? 600L * 1024 * 1024 : isM ? 700L * 1024 * 1024 : isL ? 800L * 1024 * 1024 : isXL ? 900L * 1024 * 1024 : 700L * 1024 * 1024;

            // Default size for 3B models with unknown quantization
            return 500L * 1024 * 1024;
        }
        else if (artifactName.Contains("7B", StringComparison.OrdinalIgnoreCase) ||
                 artifactName.Contains("7-B", StringComparison.OrdinalIgnoreCase))
        {
            // 7B models
            if (hasQ2) return isXS ? 600L * 1024 * 1024 : isS ? 700L * 1024 * 1024 : isM ? 800L * 1024 * 1024 : isL ? 900L * 1024 * 1024 : isXL ? 1000L * 1024 * 1024 : 800L * 1024 * 1024;
            if (hasQ3) return isXS ? 700L * 1024 * 1024 : isS ? 800L * 1024 * 1024 : isM ? 950L * 1024 * 1024 : isL ? 1100L * 1024 * 1024 : isXL ? 1300L * 1024 * 1024 : 950L * 1024 * 1024;
            if (hasQ4) return isXS ? 800L * 1024 * 1024 : isS ? 950L * 1024 * 1024 : isM ? 1100L * 1024 * 1024 : isL ? 1300L * 1024 * 1024 : isXL ? 1500L * 1024 * 1024 : 1100L * 1024 * 1024;
            if (hasQ5) return isXS ? 900L * 1024 * 1024 : isS ? 1100L * 1024 * 1024 : isM ? 1300L * 1024 * 1024 : isL ? 1500L * 1024 * 1024 : isXL ? 1700L * 1024 * 1024 : 1300L * 1024 * 1024;
            if (hasQ6) return isXS ? 1000L * 1024 * 1024 : isS ? 1200L * 1024 * 1024 : isM ? 1400L * 1024 * 1024 : isL ? 1600L * 1024 * 1024 : isXL ? 1800L * 1024 * 1024 : 1400L * 1024 * 1024;
            if (hasQ8) return isXS ? 1200L * 1024 * 1024 : isS ? 1400L * 1024 * 1024 : isM ? 1600L * 1024 * 1024 : isL ? 1800L * 1024 * 1024 : isXL ? 2000L * 1024 * 1024 : 1600L * 1024 * 1024;

            // Default size for 7B models with unknown quantization
            return 1200L * 1024 * 1024;
        }
        else if (artifactName.Contains("13B", StringComparison.OrdinalIgnoreCase) ||
                 artifactName.Contains("13-B", StringComparison.OrdinalIgnoreCase))
        {
            // 13B models
            if (hasQ2) return isXS ? 1200L * 1024 * 1024 : isS ? 1400L * 1024 * 1024 : isM ? 1600L * 1024 * 1024 : isL ? 1800L * 1024 * 1024 : isXL ? 2000L * 1024 * 1024 : 1600L * 1024 * 1024;
            if (hasQ3) return isXS ? 1400L * 1024 * 1024 : isS ? 1600L * 1024 * 1024 : isM ? 1800L * 1024 * 1024 : isL ? 2000L * 1024 * 1024 : isXL ? 2400L * 1024 * 1024 : 1800L * 1024 * 1024;
            if (hasQ4) return isXS ? 1600L * 1024 * 1024 : isS ? 1800L * 1024 * 1024 : isM ? 2000L * 1024 * 1024 : isL ? 2400L * 1024 * 1024 : isXL ? 2800L * 1024 * 1024 : 2000L * 1024 * 1024;
            if (hasQ5) return isXS ? 1800L * 1024 * 1024 : isS ? 2000L * 1024 * 1024 : isM ? 2400L * 1024 * 1024 : isL ? 2800L * 1024 * 1024 : isXL ? 3200L * 1024 * 1024 : 2400L * 1024 * 1024;
            if (hasQ6) return isXS ? 2000L * 1024 * 1024 : isS ? 2200L * 1024 * 1024 : isM ? 2600L * 1024 * 1024 : isL ? 3000L * 1024 * 1024 : isXL ? 3400L * 1024 * 1024 : 2600L * 1024 * 1024;
            if (hasQ8) return isXS ? 2200L * 1024 * 1024 : isS ? 2600L * 1024 * 1024 : isM ? 3000L * 1024 * 1024 : isL ? 3400L * 1024 * 1024 : isXL ? 3800L * 1024 * 1024 : 3000L * 1024 * 1024;

            // Default size for 13B models with unknown quantization
            return 2400L * 1024 * 1024;
        }
        else if (artifactName.Contains("70B", StringComparison.OrdinalIgnoreCase) ||
                 artifactName.Contains("70-B", StringComparison.OrdinalIgnoreCase))
        {
            // 70B models
            if (hasQ2) return isXS ? 6L * 1024 * 1024 * 1024 : isS ? 7L * 1024 * 1024 * 1024 : isM ? 8L * 1024 * 1024 * 1024 : isL ? 9L * 1024 * 1024 * 1024 : isXL ? 10L * 1024 * 1024 * 1024 : 8L * 1024 * 1024 * 1024;
            if (hasQ3) return isXS ? 7L * 1024 * 1024 * 1024 : isS ? 8L * 1024 * 1024 * 1024 : isM ? 9L * 1024 * 1024 * 1024 : isL ? 10L * 1024 * 1024 * 1024 : isXL ? 12L * 1024 * 1024 * 1024 : 9L * 1024 * 1024 * 1024;
            if (hasQ4) return isXS ? 8L * 1024 * 1024 * 1024 : isS ? 9L * 1024 * 1024 * 1024 : isM ? 10L * 1024 * 1024 * 1024 : isL ? 12L * 1024 * 1024 * 1024 : isXL ? 14L * 1024 * 1024 * 1024 : 10L * 1024 * 1024 * 1024;
            if (hasQ5) return isXS ? 9L * 1024 * 1024 * 1024 : isS ? 10L * 1024 * 1024 * 1024 : isM ? 12L * 1024 * 1024 * 1024 : isL ? 14L * 1024 * 1024 * 1024 : isXL ? 16L * 1024 * 1024 * 1024 : 12L * 1024 * 1024 * 1024;
            if (hasQ6) return isXS ? 10L * 1024 * 1024 * 1024 : isS ? 11L * 1024 * 1024 * 1024 : isM ? 13L * 1024 * 1024 * 1024 : isL ? 15L * 1024 * 1024 * 1024 : isXL ? 17L * 1024 * 1024 * 1024 : 13L * 1024 * 1024 * 1024;
            if (hasQ8) return isXS ? 11L * 1024 * 1024 * 1024 : isS ? 13L * 1024 * 1024 * 1024 : isM ? 15L * 1024 * 1024 * 1024 : isL ? 17L * 1024 * 1024 * 1024 : isXL ? 19L * 1024 * 1024 * 1024 : 15L * 1024 * 1024 * 1024;

            // Default size for 70B models with unknown quantization
            return 12L * 1024 * 1024 * 1024;
        }
        else
        {
            // Default size estimation for other models
            // Start with a moderate size based on typical models
            baseSize = 1L * 1024 * 1024 * 1024; // 1GB as default

            // Apply quantization adjustments (approximate)
            if (hasQ2) baseSize = baseSize / 16;
            else if (hasQ3) baseSize = baseSize / 12;
            else if (hasQ4) baseSize = baseSize / 8;
            else if (hasQ5) baseSize = baseSize / 6;
            else if (hasQ6) baseSize = baseSize / 5;
            else if (hasQ8) baseSize = baseSize / 4;

            // Apply size category adjustments
            if (isXS) baseSize = (long)(baseSize * 0.6);
            else if (isS) baseSize = (long)(baseSize * 0.8);
            else if (isM) baseSize = baseSize;
            else if (isL) baseSize = (long)(baseSize * 1.2);
            else if (isXL) baseSize = (long)(baseSize * 1.5);
        }

        return baseSize;
    }

    /// <summary>
    /// Helper method to determine the model type from a repository ID
    /// </summary>
    public static async Task<ModelType> DetermineModelTypeAsync(string repoId, HuggingFaceClient client, CancellationToken cancellationToken)
    {
        try
        {
            var hfModel = await client.FindModelByRepoIdAsync(repoId, cancellationToken);
            return DetermineModelType(hfModel);
        }
        catch
        {
            // Default to text generation if we can't determine
            return ModelType.TextGeneration;
        }
    }

    /// <summary>
    /// Estimates the size of a multi-file artifact
    /// </summary>
    private static long EstimateMultiFileArtifactSize(string artifactName, string format, int partCount)
    {
        var baseSize = EstimateArtifactSize(artifactName, format);

        // For multi-file artifacts, we assume the total size is distributed across all parts
        return baseSize;
    }

    /// <summary>
    /// Checks if a file extension is for a model file
    /// </summary>
    private static bool IsModelFileExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension))
            return false;

        extension = extension.ToLowerInvariant();
        return extension == ".gguf" ||
               extension == ".bin" ||
               extension == ".safetensors" ||
               extension == ".ggml" ||
               extension == ".pt" ||
               extension == ".pth";
    }

    /// <summary>
    /// Extracts artifacts directly from siblings information
    /// </summary>
    public static List<ModelArtifact> ExtractArtifactsFromSiblings(List<Sibling> siblings, string defaultFormat)
    {
        var artifacts = new List<ModelArtifact>();

        if (siblings == null || siblings.Count == 0)
        {
            return artifacts;
        }

        // First, collect all GGUF files
        var ggufFiles = siblings.Where(s => !string.IsNullOrEmpty(s.Filename) &&
                                           s.Filename.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
                               .Select(s => s.Filename)
                               .ToList();

        if (ggufFiles.Count > 0)
        {
            // Process GGUF files first
            foreach (var file in ggufFiles)
            {
                var extension = Path.GetExtension(file);
                var format = extension.TrimStart('.').ToLowerInvariant();
                var baseName = Path.GetFileNameWithoutExtension(file);

                // Check for multi-file patterns
                var multiFileMatch = _multiFileRegex.Match(baseName);
                if (multiFileMatch.Success)
                {
                    // Handle multi-file artifacts - skip for now as we'll process them separately
                    continue;
                }

                // Parse quantization and size information
                int? quantBits = null;
                string? sizeCategory = null;

                // Check for quantization information
                var quantMatch = _quantizationRegex.Match(baseName);
                if (quantMatch.Success)
                {
                    quantBits = int.Parse(quantMatch.Groups[1].Value);
                }

                // Check for size category
                var sizeMatch = _sizeCategoryRegex.Match(baseName);
                if (sizeMatch.Success)
                {
                    sizeCategory = GetNormalizedSizeCategory(sizeMatch.Groups[1].Value);
                }

                artifacts.Add(new ModelArtifact
                {
                    Name = baseName,
                    Format = format,
                    FilePaths = new List<string> { file },
                    Description = GetArtifactDescription(baseName, format),
                    QuantizationBits = quantBits,
                    SizeCategory = sizeCategory,
                    SizeInBytes = EstimateArtifactSize(baseName, format)
                });
            }

            // Now process multi-file artifacts
            var fileGroups = new Dictionary<string, List<string>>();
            foreach (var file in ggufFiles)
            {
                var baseName = Path.GetFileNameWithoutExtension(file);
                var multiFileMatch = _multiFileRegex.Match(baseName);

                if (multiFileMatch.Success)
                {
                    var artifactBaseName = multiFileMatch.Groups[1].Value;

                    if (!fileGroups.TryGetValue(artifactBaseName, out var group))
                    {
                        group = new List<string>();
                        fileGroups[artifactBaseName] = group;
                    }

                    group.Add(file);
                }
            }

            // Create artifacts for each multi-file group
            foreach (var group in fileGroups)
            {
                if (group.Value.Count > 0)
                {
                    var firstFile = group.Value[0];
                    var format = Path.GetExtension(firstFile).TrimStart('.').ToLowerInvariant();
                    var baseName = group.Key;

                    // Parse quantization and size information
                    int? quantBits = null;
                    string? sizeCategory = null;

                    // Check for quantization information
                    var quantMatch = _quantizationRegex.Match(baseName);
                    if (quantMatch.Success)
                    {
                        quantBits = int.Parse(quantMatch.Groups[1].Value);
                    }

                    // Check for size category
                    var sizeMatch = _sizeCategoryRegex.Match(baseName);
                    if (sizeMatch.Success)
                    {
                        sizeCategory = GetNormalizedSizeCategory(sizeMatch.Groups[1].Value);
                    }

                    artifacts.Add(new ModelArtifact
                    {
                        Name = baseName,
                        Format = format,
                        FilePaths = group.Value,
                        Description = GetArtifactDescription(baseName, format, true),
                        QuantizationBits = quantBits,
                        SizeCategory = sizeCategory,
                        SizeInBytes = EstimateMultiFileArtifactSize(baseName, format, group.Value.Count)
                    });
                }
            }
        }

        // If no GGUF files, try other formats
        if (artifacts.Count == 0)
        {
            // Try other common formats: bin, safetensors, pt, pth
            var modelFiles = siblings.Where(s => !string.IsNullOrEmpty(s.Filename) &&
                                              (s.Filename.EndsWith(".bin", StringComparison.OrdinalIgnoreCase) ||
                                               s.Filename.EndsWith(".safetensors", StringComparison.OrdinalIgnoreCase) ||
                                               s.Filename.EndsWith(".pt", StringComparison.OrdinalIgnoreCase) ||
                                               s.Filename.EndsWith(".pth", StringComparison.OrdinalIgnoreCase)))
                                  .Select(s => s.Filename)
                                  .ToList();

            foreach (var file in modelFiles)
            {
                var extension = Path.GetExtension(file);
                var format = extension.TrimStart('.').ToLowerInvariant();
                var baseName = Path.GetFileNameWithoutExtension(file);

                artifacts.Add(new ModelArtifact
                {
                    Name = baseName,
                    Format = format,
                    FilePaths = new List<string> { file },
                    Description = $"{format.ToUpperInvariant()} format model",
                    SizeInBytes = EstimateArtifactSize(baseName, format)
                });
            }
        }

        // If still no artifacts, create a placeholder with the first file
        if (artifacts.Count == 0 && siblings.Count > 0)
        {
            // Find first non-empty filename
            var firstFilename = siblings.FirstOrDefault(s => !string.IsNullOrEmpty(s.Filename))?.Filename;

            if (!string.IsNullOrEmpty(firstFilename))
            {
                var extension = Path.GetExtension(firstFilename);
                var format = !string.IsNullOrEmpty(extension) ? extension.TrimStart('.').ToLowerInvariant() : defaultFormat;
                var baseName = Path.GetFileNameWithoutExtension(firstFilename);

                artifacts.Add(new ModelArtifact
                {
                    Name = baseName,
                    Format = format,
                    FilePaths = new List<string> { firstFilename },
                    Description = $"Default model file",
                    SizeInBytes = EstimateArtifactSize(baseName, format)
                });
            }
        }

        return artifacts;
    }

    /// <summary>
    /// Finds files in a HuggingFace model that match an artifact name
    /// </summary>
    public static List<string> FindArtifactFiles(HuggingFaceModel model, string artifactName)
    {
        if (model == null || string.IsNullOrEmpty(artifactName))
            return new List<string>();

        // Get all model weight files
        var allFiles = model.GetModelWeightPaths();

        // Start with exact matches (file name without extension)
        var exactMatches = allFiles.Where(f =>
            Path.GetFileNameWithoutExtension(f).Equals(artifactName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (exactMatches.Count > 0)
            return exactMatches;

        // Try files that start with the artifact name
        var startingMatches = allFiles.Where(f =>
            Path.GetFileNameWithoutExtension(f).StartsWith(artifactName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (startingMatches.Count > 0)
            return startingMatches;

        // Fallback to files that contain the artifact name
        var containingMatches = allFiles.Where(f =>
            Path.GetFileNameWithoutExtension(f).Contains(artifactName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (containingMatches.Count > 0)
            return containingMatches;

        // As a last resort, try common extensions for the exact artifact name
        return new List<string> { $"{artifactName}.gguf" };
    }

    /// <summary>
    /// Calculates the size of a specific artifact in a model
    /// </summary>
    public static long? CalculateArtifactSize(HuggingFaceModel hfModel, string artifactName)
    {
        if (hfModel == null || string.IsNullOrEmpty(artifactName))
            return null;

        // Find files that match the artifact name
        var matchingFiles = FindArtifactFiles(hfModel, artifactName);

        if (matchingFiles.Count == 0)
            return EstimateArtifactSize(artifactName, "gguf");

        // Calculate total size of matching files
        long totalSize = 0;
        foreach (var file in matchingFiles)
        {
            // Try to get actual file size from the model if available
            var fileSize = EstimateFileSize(hfModel, file);
            totalSize += fileSize;
        }

        return totalSize;
    }
}