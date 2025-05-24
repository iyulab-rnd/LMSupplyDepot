namespace LMSupplyDepots.ModelHub.Utils;

/// <summary>
/// Provides utilities for managing model directory and file structures
/// </summary>
public static class FileSystemHelper
{
    /// <summary>
    /// Downloads directory name within the models directory
    /// </summary>
    public const string DownloadsDirectory = ".downloads";

    /// <summary>
    /// Default file extension for model files (GGUF format)
    /// </summary>
    public const string ModelFileExtension = ".gguf";

    /// <summary>
    /// File extension for model metadata files
    /// </summary>
    public const string MetadataFileExtension = ".json";

    /// <summary>
    /// File extension for download status files
    /// </summary>
    public const string DownloadStatusFileExtension = ".download";

    /// <summary>
    /// Model file formats in order of preference
    /// </summary>
    public static readonly string[] PreferredModelFormats = [
        ".gguf",       // GGUF for local inference
        ".safetensors", // SafeTensors (more widely supported, safer to load)
        ".bin"         // Standard HuggingFace binary format
    ];

    /// <summary>
    /// Gets the path to the status file for a downloading model file
    /// </summary>
    public static string GetDownloadStatusFilePath(string modelId, string artifactName, ModelType modelType, string basePath)
    {
        // If we have a specific artifact name
        if (!string.IsNullOrEmpty(artifactName))
        {
            // Create the status file alongside where the model will be downloaded
            var sanitizedArtifactName = SanitizeFileNamePart(artifactName);

            // Get the target directory for this model/artifact
            var modelDir = GetModelDirectoryPath(modelId, modelType, basePath);
            Directory.CreateDirectory(modelDir);

            // Use format: {modelDir}/{artifactName}.download
            return Path.Combine(modelDir, $"{sanitizedArtifactName}{DownloadStatusFileExtension}");
        }

        // For full model downloads without a specific artifact
        if (ModelIdentifier.TryParse(modelId, out var identifier))
        {
            var structure = GetModelFileStructure(identifier, basePath);
            Directory.CreateDirectory(structure.ModelNamePath);

            // Place in the model directory
            var fileName = $"{identifier.ModelName}{DownloadStatusFileExtension}";
            return Path.Combine(structure.ModelNamePath, fileName);
        }

        // Legacy fallback - place in .downloads directory
        var safeFileName = SanitizeFileNamePart(modelId);
        var legacyDownloadDir = Path.Combine(basePath, DownloadsDirectory);
        Directory.CreateDirectory(legacyDownloadDir);
        return Path.Combine(legacyDownloadDir, $"{safeFileName}{DownloadStatusFileExtension}");
    }

    /// <summary>
    /// Gets the model directory path
    /// </summary>
    public static string GetModelDirectoryPath(string modelId, ModelType modelType, string basePath)
    {
        if (ModelIdentifier.TryParse(modelId, out var identifier))
        {
            // Apply the model type from parameter
            identifier = identifier.WithModelType(modelType);
            return GetModelDirectoryPath(identifier, basePath);
        }

        // Legacy fallback for backward compatibility
        var typeDashCase = modelType.ToString().ToLowerInvariant().Replace("_", "-");

        // If model ID has repo/artifact format, handle accordingly
        if (modelId.Contains('/'))
        {
            var parts = modelId.Split('/');

            if (parts.Length >= 3 && modelId.Contains(':'))
            {
                // hf:owner/repo/artifact format
                var registry = parts[0].Replace(":", "");
                var publisher = parts[1];
                var repo = parts[2];

                return Path.Combine(basePath, "models", typeDashCase, publisher, repo);
            }
            else if (parts.Length >= 2)
            {
                // owner/repo format
                var publisher = parts[0];
                var repo = parts[1];

                return Path.Combine(basePath, "models", typeDashCase, publisher, repo);
            }
        }

        // If no clear structure, use a simple path
        return Path.Combine(basePath, "models", typeDashCase, "local", modelId);
    }

    /// <summary>
    /// Gets the path to the status file for a downloading model
    /// </summary>
    public static string GetDownloadStatusFilePath(ModelIdentifier modelId, string basePath)
    {
        var structure = GetModelFileStructure(modelId, basePath);
        var fileName = $"{modelId.ArtifactName}{DownloadStatusFileExtension}";
        return Path.Combine(structure.ModelNamePath, fileName);
    }

    /// <summary>
    /// Sanitizes a file name part by replacing invalid characters
    /// </summary>
    private static string SanitizeFileNamePart(string input)
    {
        if (string.IsNullOrEmpty(input))
            return "unknown";

        // Replace characters that are invalid in file names
        var invalidChars = Path.GetInvalidFileNameChars();
        var result = new string(input.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());

        // Ensure we don't have too many consecutive underscores
        while (result.Contains("__"))
        {
            result = result.Replace("__", "_");
        }

        return result;
    }

    /// <summary>
    /// Gets information about the directory structure for a model
    /// </summary>
    public static ModelFileStructure GetModelFileStructure(ModelIdentifier modelId, string basePath)
    {
        // Get model type in dash-case (e.g., "text-generation")
        var modelTypeDashCase = modelId.ModelType.ToString().ToLowerInvariant().Replace("_", "-");

        // Set up paths
        var modelsPath = Path.Combine(basePath, "models");
        var modelTypePath = Path.Combine(modelsPath, modelTypeDashCase);
        var publisherPath = Path.Combine(modelTypePath, modelId.Publisher);
        var modelNamePath = Path.Combine(publisherPath, modelId.ModelName);

        return new ModelFileStructure
        {
            BasePath = basePath,
            ModelsPath = modelsPath,
            ModelTypePath = modelTypePath,
            PublisherPath = publisherPath,
            ModelNamePath = modelNamePath,

            ModelId = modelId.ToString(),
            Registry = modelId.Registry,
            Publisher = modelId.Publisher,
            ModelName = modelId.ModelName,
            ArtifactName = modelId.ArtifactName,
            Format = modelId.Format,
            ModelType = modelId.ModelType
        };
    }

    /// <summary>
    /// Gets the base model directory path
    /// </summary>
    public static string GetModelDirectoryPath(ModelIdentifier modelId, string basePath)
    {
        var structure = GetModelFileStructure(modelId, basePath);
        return structure.ModelNamePath;
    }

    /// <summary>
    /// Gets the path to the metadata file for a model
    /// </summary>
    public static string GetMetadataFilePath(ModelIdentifier modelId, string basePath)
    {
        var structure = GetModelFileStructure(modelId, basePath);
        string fileName = $"{modelId.ArtifactName}{MetadataFileExtension}";
        return Path.Combine(structure.ModelNamePath, fileName);
    }

    /// <summary>
    /// Creates all necessary directories for a model
    /// </summary>
    public static void EnsureModelDirectoriesExist(ModelIdentifier modelId, string basePath)
    {
        var structure = GetModelFileStructure(modelId, basePath);

        // Create all directories in hierarchy
        Directory.CreateDirectory(structure.ModelsPath);
        Directory.CreateDirectory(structure.ModelTypePath);
        Directory.CreateDirectory(structure.PublisherPath);
        Directory.CreateDirectory(structure.ModelNamePath);
    }

    /// <summary>
    /// Creates the downloads directory
    /// </summary>
    public static void EnsureDownloadsDirectoryExists(string basePath)
    {
        var downloadsDir = Path.Combine(basePath, DownloadsDirectory);
        Directory.CreateDirectory(downloadsDir);
    }

    /// <summary>
    /// Ensures all base directories exist
    /// </summary>
    public static void EnsureBaseDirectoriesExist(string basePath)
    {
        // Ensure base directory exists
        Directory.CreateDirectory(basePath);

        // Ensure models directory exists
        Directory.CreateDirectory(Path.Combine(basePath, "models"));

        // Ensure downloads directory exists
        EnsureDownloadsDirectoryExists(basePath);

        // Create directories for each model type
        foreach (var modelType in Enum.GetValues<ModelType>())
        {
            var modelTypeDashCase = modelType.ToString().ToLowerInvariant().Replace("_", "-");
            var typeDirPath = Path.Combine(basePath, "models", modelTypeDashCase);
            Directory.CreateDirectory(typeDirPath);
        }
    }

    /// <summary>
    /// Finds the main model file in a directory
    /// </summary>
    public static string? FindMainModelFile(string modelDirectory)
    {
        if (!Directory.Exists(modelDirectory))
        {
            return null;
        }

        // Try each preferred format in order
        foreach (var format in PreferredModelFormats)
        {
            var files = Directory.GetFiles(modelDirectory, $"*{format}", SearchOption.TopDirectoryOnly);
            if (files.Length > 0)
            {
                // Return the largest file with this format
                return files.OrderByDescending(f => new FileInfo(f).Length).First();
            }
        }

        // No model files found
        return null;
    }

    /// <summary>
    /// Checks if a directory contains valid model files
    /// </summary>
    public static bool ContainsModelFiles(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return false;
        }

        foreach (var format in PreferredModelFormats)
        {
            if (Directory.GetFiles(directory, $"*{format}", SearchOption.TopDirectoryOnly).Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Verifies that a model file exists and returns its actual path
    /// </summary>
    public static string? VerifyModelFilePath(string modelPath, ModelType modelType)
    {
        // If it's a direct file path
        if (File.Exists(modelPath))
        {
            string extension = Path.GetExtension(modelPath).ToLowerInvariant();
            if (PreferredModelFormats.Contains(extension))
            {
                return modelPath;
            }
        }

        // If it's a directory, try to find the main model file
        if (Directory.Exists(modelPath))
        {
            return FindMainModelFile(modelPath);
        }

        return null;
    }

    /// <summary>
    /// Gets all model files in a directory with their sizes
    /// </summary>
    public static Dictionary<string, long> GetModelFilesWithSizes(string directory)
    {
        var result = new Dictionary<string, long>();

        if (!Directory.Exists(directory))
        {
            return result;
        }

        foreach (var format in PreferredModelFormats)
        {
            foreach (var file in Directory.GetFiles(directory, $"*{format}", SearchOption.TopDirectoryOnly))
            {
                long size = new FileInfo(file).Length;
                result[file] = size;
            }
        }

        return result;
    }

    /// <summary>
    /// Determines if a file is likely a model file based on extension and minimum size
    /// </summary>
    public static bool IsLikelyModelFile(string filePath, long minimumSize = 1024 * 1024) // 1MB minimum
    {
        if (!File.Exists(filePath))
        {
            return false;
        }

        string extension = Path.GetExtension(filePath).ToLowerInvariant();
        if (!PreferredModelFormats.Contains(extension))
        {
            return false;
        }

        var fileInfo = new FileInfo(filePath);
        return fileInfo.Length >= minimumSize;
    }
}