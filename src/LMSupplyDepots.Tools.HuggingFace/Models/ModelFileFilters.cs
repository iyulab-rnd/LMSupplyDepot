using System.Text.RegularExpressions;

namespace LMSupplyDepots.Tools.HuggingFace.Models;

/// <summary>
/// Provides extension methods and filters for HuggingFace model files.
/// </summary>
public static class ModelFileFilters
{
    /// <summary>
    /// Regular expression pattern for matching model weight files.
    /// </summary>
    public static readonly Regex ModelWeightPattern = new(
        @"\.(bin|safetensors|gguf|pt|pth|ckpt|model)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Regular expression pattern for matching GGUF model files.
    /// </summary>
    public static readonly Regex GgufModelPattern = new(
        @"\.gguf$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Regular expression pattern for matching model configuration files.
    /// </summary>
    public static readonly Regex ConfigFilePattern = new(
        @"\.(json|yaml|yml)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Regular expression pattern for matching tokenizer files.
    /// </summary>
    public static readonly Regex TokenizerFilePattern = new(
        @"tokenizer\.(json|model)$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Gets the paths of model weight files in the repository.
    /// </summary>
    /// <param name="model">The HuggingFace model.</param>
    /// <returns>An array of file paths matching the model weight pattern.</returns>
    public static string[] GetModelWeightPaths(this HuggingFaceModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.GetFilePaths(ModelWeightPattern);
    }

    /// <summary>
    /// Gets the paths of GGUF model files in the repository.
    /// </summary>
    /// <param name="model">The HuggingFace model.</param>
    /// <returns>An array of file paths matching the GGUF model pattern.</returns>
    public static string[] GetGgufModelPaths(this HuggingFaceModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.GetFilePaths(GgufModelPattern);
    }

    /// <summary>
    /// Checks if the model has any GGUF format files.
    /// </summary>
    /// <param name="model">The HuggingFace model.</param>
    /// <returns>True if the model contains GGUF files, false otherwise.</returns>
    public static bool HasGgufFiles(this HuggingFaceModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.GetGgufModelPaths().Length > 0;
    }

    /// <summary>
    /// Gets the paths of configuration files in the repository.
    /// </summary>
    /// <param name="model">The HuggingFace model.</param>
    /// <returns>An array of file paths matching the configuration file pattern.</returns>
    public static string[] GetConfigurationPaths(this HuggingFaceModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.GetFilePaths(ConfigFilePattern);
    }

    /// <summary>
    /// Gets the paths of tokenizer files in the repository.
    /// </summary>
    /// <param name="model">The HuggingFace model.</param>
    /// <returns>An array of file paths matching the tokenizer file pattern.</returns>
    public static string[] GetTokenizerPaths(this HuggingFaceModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return model.GetFilePaths(TokenizerFilePattern);
    }

    /// <summary>
    /// Gets the paths of essential model files (weights, configurations, and tokenizers) in the repository.
    /// </summary>
    /// <param name="model">The HuggingFace model.</param>
    /// <returns>An array of file paths for essential model files.</returns>
    public static string[] GetEssentialModelPaths(this HuggingFaceModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var weightFiles = model.GetModelWeightPaths();
        var configFiles = model.GetConfigurationPaths();
        var tokenizerFiles = model.GetTokenizerPaths();

        return weightFiles
            .Concat(configFiles)
            .Concat(tokenizerFiles)
            .Distinct()
            .ToArray();
    }
}