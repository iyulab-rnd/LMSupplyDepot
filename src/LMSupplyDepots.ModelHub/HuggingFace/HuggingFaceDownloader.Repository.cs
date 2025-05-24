using LMSupplyDepots.External.HuggingFace.Models;
using System.Text.Json;

namespace LMSupplyDepots.ModelHub.HuggingFace;

/// <summary>
/// Implementation of repository information operations
/// </summary>
public partial class HuggingFaceDownloader
{
    /// <summary>
    /// Gets information about a model from Hugging Face without downloading it
    /// </summary>
    public async Task<LMModel> GetModelInfoAsync(string modelId, CancellationToken cancellationToken = default)
    {
        // Explicitly parse the ID to extract repo ID and artifact name
        var (repoId, artifactName) = HuggingFaceHelper.NormalizeAndSplitSourceId(modelId);

        // Get repository information
        var repo = await GetRepositoryInfoAsync(repoId, cancellationToken);

        _logger.LogInformation("Repository {RepoId} has {Count} available artifacts",
            repoId, repo.AvailableArtifacts.Count);

        // Log all available artifacts for debugging
        if (repo.AvailableArtifacts.Count > 0)
        {
            _logger.LogInformation("Available artifacts in {RepoId}: {Artifacts}",
                repoId, string.Join(", ", repo.AvailableArtifacts.Select(a => a.Name)));
        }

        // If a specific artifact was requested
        if (!string.IsNullOrEmpty(artifactName))
        {
            // Normalize the artifactName to handle potential format issues
            string normalizedArtifactName = Path.GetFileNameWithoutExtension(artifactName);

            _logger.LogInformation("Looking for artifact '{ArtifactName}' (normalized to '{NormalizedName}') in repository '{RepoId}'",
                artifactName, normalizedArtifactName, repoId);

            // First try exact match
            var model = repo.GetModel(normalizedArtifactName);
            if (model != null)
            {
                _logger.LogInformation("Found exact match for artifact '{ArtifactName}'", normalizedArtifactName);
                return model;
            }

            // If not found, try case-insensitive match
            var artifactList = repo.AvailableArtifacts;
            var matchingArtifact = artifactList.FirstOrDefault(a =>
                a.Name.Equals(normalizedArtifactName, StringComparison.OrdinalIgnoreCase));

            if (matchingArtifact != null)
            {
                _logger.LogInformation("Found case-insensitive match for artifact '{ArtifactName}': '{MatchingName}'",
                    normalizedArtifactName, matchingArtifact.Name);
                model = Utils.ModelFactory.FromRepositoryAndArtifact(repo, matchingArtifact);
                return model;
            }

            // If still not found, look for the most similar artifact
            var mostSimilarArtifact = FindMostSimilarArtifact(artifactList, normalizedArtifactName);
            if (mostSimilarArtifact != null)
            {
                _logger.LogInformation("Found similar artifact '{SimilarName}' for requested '{ArtifactName}'",
                    mostSimilarArtifact.Name, normalizedArtifactName);
                model = Utils.ModelFactory.FromRepositoryAndArtifact(repo, mostSimilarArtifact);
                return model;
            }

            // If repo has no artifacts but we have a specific request, create a placeholder artifact
            if (repo.AvailableArtifacts.Count == 0)
            {
                _logger.LogWarning("No artifacts found in repository '{RepoId}', creating placeholder for '{ArtifactName}'",
                    repoId, normalizedArtifactName);

                var placeholder = new ModelArtifact
                {
                    Name = normalizedArtifactName,
                    Format = repo.DefaultFormat,
                    Description = $"Placeholder for {normalizedArtifactName}",
                    SizeInBytes = 1024 * 1024 * 1024, // 1 GB placeholder
                    FilePaths = new List<string>()
                };

                repo.AvailableArtifacts.Add(placeholder);
                model = Utils.ModelFactory.FromRepositoryAndArtifact(repo, placeholder);
                return model;
            }

            // If the specific artifact wasn't found, show available artifacts in the error message
            var availableArtifacts = string.Join(", ", repo.AvailableArtifacts.Select(a => a.Name));
            throw new ModelNotFoundException(
                artifactName,
                $"Artifact '{artifactName}' not found in repository '{repoId}'. Available artifacts: {availableArtifacts}");
        }

        // Otherwise, return the recommended model from the repository
        var recommendedModel = repo.GetRecommendedModel();
        if (recommendedModel != null)
        {
            _logger.LogInformation("Using recommended model from repository '{RepoId}'", repoId);
            return recommendedModel;
        }

        // If no models are available, throw an exception
        throw new ModelNotFoundException(repoId, $"No models found in repository '{repoId}'");
    }

    /// <summary>
    /// Finds the most similar artifact to the requested name
    /// </summary>
    private ModelArtifact? FindMostSimilarArtifact(List<ModelArtifact> artifacts, string requestedName)
    {
        if (artifacts == null || artifacts.Count == 0 || string.IsNullOrEmpty(requestedName))
            return null;

        // Try to find artifacts that contain the requested name or vice versa
        var containsArtifact = artifacts.FirstOrDefault(a =>
            a.Name.Contains(requestedName, StringComparison.OrdinalIgnoreCase) ||
            requestedName.Contains(a.Name, StringComparison.OrdinalIgnoreCase));

        if (containsArtifact != null)
            return containsArtifact;

        // Look for artifacts with common prefixes (useful for versioned models)
        var normalizedRequest = requestedName.ToLowerInvariant()
            .Replace("-", "_")
            .Replace(".", "_");

        foreach (var artifact in artifacts)
        {
            var normalizedName = artifact.Name.ToLowerInvariant()
                .Replace("-", "_")
                .Replace(".", "_");

            // Check if they have a common prefix
            var minLength = Math.Min(normalizedName.Length, normalizedRequest.Length);
            int matchingPrefixLength = 0;

            for (int i = 0; i < minLength; i++)
            {
                if (normalizedName[i] == normalizedRequest[i])
                    matchingPrefixLength++;
                else
                    break;
            }

            // If more than half of the shorter name matches, consider it similar enough
            if (matchingPrefixLength > minLength / 2)
                return artifact;
        }

        // Last resort: return the first artifact with matching format if possible
        var requestedFormat = "";
        if (requestedName.Contains('.'))
        {
            requestedFormat = Path.GetExtension(requestedName).TrimStart('.').ToLowerInvariant();
        }

        if (!string.IsNullOrEmpty(requestedFormat))
        {
            var formatMatch = artifacts.FirstOrDefault(a => a.Format.Equals(requestedFormat, StringComparison.OrdinalIgnoreCase));
            if (formatMatch != null)
                return formatMatch;
        }

        // If no similar artifact found, return null
        return null;
    }

    /// <summary>
    /// Gets information about a model repository
    /// </summary>
    public async Task<LMRepo> GetRepositoryInfoAsync(string repoId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting repository information for {RepoId}", repoId);

        try
        {
            var hfModel = await _client.Value.FindModelByRepoIdAsync(repoId, cancellationToken);

            // Create the repository object
            var repo = new LMRepo
            {
                Id = $"hf:{repoId}",
                Registry = "hf",
                RepoId = repoId,
                Name = string.IsNullOrEmpty(repoId) ? "Unknown Model" : Path.GetFileName(repoId),
                Type = HuggingFaceHelper.DetermineModelType(hfModel),
                DefaultFormat = HuggingFaceHelper.GetModelFormat(hfModel),
                Version = hfModel.LastModified.ToString("yyyyMMdd"),
                Description = HuggingFaceHelper.GetModelDescription(hfModel),
                Publisher = hfModel.Author,
                Capabilities = new LMModelCapabilities
                {
                    SupportsTextGeneration = HuggingFaceHelper.DetermineModelType(hfModel) == ModelType.TextGeneration,
                    SupportsEmbeddings = HuggingFaceHelper.DetermineModelType(hfModel) == ModelType.Embedding,
                    SupportsImageUnderstanding = hfModel.Tags.Any(t =>
                        t.Contains("vision") || t.Contains("image") || t.Contains("multimodal")),
                    MaxContextLength = HuggingFaceHelper.GetMaxContextLength(hfModel),
                    EmbeddingDimension = HuggingFaceHelper.GetEmbeddingDimension(hfModel)
                }
            };

            // Try to get files using GetRepositoryFilesAsync first
            List<string> files = new List<string>();
            try
            {
                var fileInfos = await _client.Value.GetRepositoryFilesAsync(repoId, null, cancellationToken);

                // Extract file paths
                foreach (var fileInfo in fileInfos)
                {
                    if (fileInfo.IsFile)
                    {
                        files.Add(fileInfo.Path);
                    }
                }

                _logger.LogInformation("Retrieved {Count} files from repository {RepoId} using API",
                    files.Count, repoId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get repository files for {RepoId} using API. Will try using siblings info.", repoId);
                files = new List<string>();
            }

            // If we didn't get any files from the API, try using siblings info
            if (files.Count == 0 && hfModel.Siblings != null && hfModel.Siblings.Count > 0)
            {
                _logger.LogInformation("Using siblings information to extract files for {RepoId}", repoId);

                foreach (var sibling in hfModel.Siblings)
                {
                    if (!string.IsNullOrEmpty(sibling.Filename))
                    {
                        files.Add(sibling.Filename);
                    }
                }

                _logger.LogInformation("Retrieved {Count} files from siblings info for {RepoId}",
                    files.Count, repoId);
            }

            // Filter out non-empty files
            files = files.Where(f => !string.IsNullOrWhiteSpace(f)).ToList();

            // Extract artifact information
            repo.AvailableArtifacts = HuggingFaceHelper.ExtractArtifacts(files, repo.DefaultFormat);

            _logger.LogInformation("Extracted {Count} artifacts from {Files} files for {RepoId}",
                repo.AvailableArtifacts.Count, files.Count, repoId);

            // If we still don't have any artifacts but have siblings, create artifacts directly from siblings
            if (repo.AvailableArtifacts.Count == 0 && hfModel.Siblings != null && hfModel.Siblings.Count > 0)
            {
                _logger.LogInformation("Creating artifacts directly from siblings for {RepoId}", repoId);
                repo.AvailableArtifacts = HuggingFaceHelper.ExtractArtifactsFromSiblings(hfModel.Siblings, repo.DefaultFormat);

                _logger.LogInformation("Created {Count} artifacts directly from siblings for {RepoId}",
                    repo.AvailableArtifacts.Count, repoId);
            }

            // Last resort: If we still don't have any artifacts, create a placeholder
            if (repo.AvailableArtifacts.Count == 0)
            {
                _logger.LogWarning("No artifacts found for {RepoId}, creating placeholder artifact", repoId);
                repo.AvailableArtifacts.Add(new ModelArtifact
                {
                    Name = Path.GetFileName(repoId),
                    Format = repo.DefaultFormat,
                    Description = $"Default {repo.DefaultFormat} model",
                    SizeInBytes = 1024 * 1024 * 1024, // 1 GB placeholder
                    FilePaths = new List<string>()
                });
            }

            return repo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting repository information for {RepoId}", repoId);
            throw new ModelHubException($"Failed to get repository information for '{repoId}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Searches for model repositories in Hugging Face
    /// </summary>
    public async Task<IReadOnlyList<LMRepo>> SearchRepositoriesAsync(
        ModelType? type = null,
        string? searchTerm = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Searching for repositories with term: {SearchTerm}, type: {Type}, limit: {Limit}",
            searchTerm, type, limit);

        try
        {
            IReadOnlyList<HuggingFaceModel> results;

            if (type == ModelType.TextGeneration)
            {
                results = await _client.Value.SearchTextGenerationModelsAsync(
                    searchTerm, null, limit, External.HuggingFace.Models.ModelSortField.Downloads, true, cancellationToken);
            }
            else if (type == ModelType.Embedding)
            {
                results = await _client.Value.SearchEmbeddingModelsAsync(
                    searchTerm, null, limit, External.HuggingFace.Models.ModelSortField.Downloads, true, cancellationToken);
            }
            else
            {
                // Search both types and combine results
                var textGenResults = await _client.Value.SearchTextGenerationModelsAsync(
                    searchTerm, null, limit / 2, External.HuggingFace.Models.ModelSortField.Downloads, true, cancellationToken);

                var embeddingResults = await _client.Value.SearchEmbeddingModelsAsync(
                    searchTerm, null, limit / 2, External.HuggingFace.Models.ModelSortField.Downloads, true, cancellationToken);

                results = [.. textGenResults.Concat(embeddingResults)
                .OrderByDescending(m => m.Downloads)
                .Take(limit)];
            }

            // Convert to LMRepo objects
            var repos = new List<LMRepo>();
            foreach (var hfModel in results)
            {
                try
                {
                    var repoId = hfModel.ModelId;
                    var modelType = HuggingFaceHelper.DetermineModelType(hfModel);
                    var format = HuggingFaceHelper.GetModelFormat(hfModel);

                    var repo = new LMRepo
                    {
                        Id = $"hf:{repoId}",
                        Registry = "hf",
                        RepoId = repoId,
                        Name = string.IsNullOrEmpty(repoId) ?
                            "Unknown Model" : Path.GetFileName(repoId),
                        Type = modelType,
                        DefaultFormat = format,
                        Version = hfModel.LastModified.ToString("yyyyMMdd"),
                        Description = HuggingFaceHelper.GetModelDescription(hfModel),
                        Publisher = hfModel.Author,
                        Capabilities = new LMModelCapabilities
                        {
                            SupportsTextGeneration = modelType == ModelType.TextGeneration,
                            SupportsEmbeddings = modelType == ModelType.Embedding,
                            SupportsImageUnderstanding = hfModel.Tags.Any(t =>
                                t.Contains("vision") || t.Contains("image") || t.Contains("multimodal")),
                            MaxContextLength = HuggingFaceHelper.GetMaxContextLength(hfModel),
                            EmbeddingDimension = HuggingFaceHelper.GetEmbeddingDimension(hfModel)
                        }
                    };

                    // Create artifacts from siblings data
                    PopulateRepositoryArtifacts(repo, hfModel);

                    repos.Add(repo);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to convert model {ModelId} to repository", hfModel.ModelId);
                }
            }

            return repos;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching repositories");
            throw new ModelHubException($"Failed to search repositories: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Populates repository artifacts based on the model's siblings
    /// </summary>
    private static void PopulateRepositoryArtifacts(LMRepo repo, HuggingFaceModel hfModel)
    {
        // Process siblings to create actual artifacts
        if (hfModel.Siblings != null && hfModel.Siblings.Any())
        {
            // Filter for model files (like GGUF files) in the siblings
            var modelFiles = hfModel.Siblings
                .Where(s => !string.IsNullOrEmpty(s.Filename) &&
                          (s.Filename.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase) ||
                           s.Filename.EndsWith(".safetensors", StringComparison.OrdinalIgnoreCase) ||
                           s.Filename.EndsWith(".bin", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            // If we found model files, create artifacts for each one
            if (modelFiles.Any())
            {
                foreach (var file in modelFiles)
                {
                    var artifactName = Path.GetFileNameWithoutExtension(file.Filename);
                    var artifactFormat = Path.GetExtension(file.Filename).TrimStart('.');

                    // Parse quantization and size information
                    int? quantBits = null;
                    string? sizeCategory = null;

                    // Check for quantization information (Q2, Q4, Q5, Q8, etc)
                    var quantMatch = System.Text.RegularExpressions.Regex.Match(
                        artifactName, @"Q(\d+)(_[KM])?(_S|_M|_L|_XL)?");
                    if (quantMatch.Success)
                    {
                        quantBits = int.Parse(quantMatch.Groups[1].Value);
                    }

                    // Check for size category (S, M, L, XL, etc)
                    var sizeMatch = System.Text.RegularExpressions.Regex.Match(
                        artifactName, @"(_XS|_S|_M|_L|_XL|-xs|-small|-medium|-large|-xl)$");
                    if (sizeMatch.Success)
                    {
                        sizeCategory = GetNormalizedSizeCategory(sizeMatch.Groups[1].Value);
                    }

                    var artifact = new ModelArtifact
                    {
                        Name = artifactName,
                        Format = artifactFormat,
                        Description = GetArtifactDescription(artifactName, artifactFormat),
                        // Use the improved size estimation method 
                        SizeInBytes = HuggingFaceHelper.EstimateArtifactSize(artifactName, artifactFormat),
                        FilePaths = new List<string> { file.Filename },
                        QuantizationBits = quantBits,
                        SizeCategory = sizeCategory
                    };

                    repo.AvailableArtifacts.Add(artifact);
                }
            }
            else
            {
                // Fallback to placeholder if no model files found
                AddPlaceholderArtifact(repo, hfModel);
            }
        }
        else
        {
            // If no siblings information, use placeholder
            AddPlaceholderArtifact(repo, hfModel);
        }
    }

    /// <summary>
    /// Adds a placeholder artifact when no specific artifacts are found
    /// </summary>
    private static void AddPlaceholderArtifact(LMRepo repo, HuggingFaceModel hfModel)
    {
        var defaultFormat = HuggingFaceHelper.GetModelFormat(hfModel);
        var artifactName = hfModel.ModelId.Split('/').Last();

        var placeholderArtifact = new ModelArtifact
        {
            Name = artifactName,
            Format = defaultFormat,
            Description = $"Default {defaultFormat} model in this repository",
            // Set approximate size - will be updated during actual info fetch
            SizeInBytes = 1024 * 1024 * 1024, // 1 GB placeholder
            FilePaths = new List<string>()
        };

        repo.AvailableArtifacts.Add(placeholderArtifact);
    }

    /// <summary>
    /// Generates a description for an artifact based on its name and format
    /// </summary>
    private static string GetArtifactDescription(string artifactName, string format)
    {
        var parts = new List<string>();

        // Add format info
        parts.Add($"{format.ToUpperInvariant()} format");

        // Check for quantization information in the name
        if (artifactName.Contains("Q2") || artifactName.Contains("Q3") ||
            artifactName.Contains("Q4") || artifactName.Contains("Q5") ||
            artifactName.Contains("Q6") || artifactName.Contains("Q8"))
        {
            var quantMatch = System.Text.RegularExpressions.Regex.Match(
                artifactName, @"Q(\d+)(_[KM])?(_S|_M|_L|_XL)?");
            if (quantMatch.Success)
            {
                parts.Add($"Q{quantMatch.Groups[1].Value} quantization");
            }
        }

        // Add size information if present
        var sizeMatch = System.Text.RegularExpressions.Regex.Match(
            artifactName, @"(_XS|_S|_M|_L|_XL|-xs|-small|-medium|-large|-xl)$");
        if (sizeMatch.Success)
        {
            var sizeCategory = GetNormalizedSizeCategory(sizeMatch.Groups[1].Value);
            parts.Add($"{sizeCategory} size");
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
}