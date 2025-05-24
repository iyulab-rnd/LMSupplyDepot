namespace LMSupplyDepots.ModelHub.Utils;

/// <summary>
/// Utility methods for creating model instances
/// </summary>
public static class ModelFactory
{
    /// <summary>
    /// Creates a model from a local file
    /// </summary>
    public static LMModel FromLocalFile(string filePath, LMModelCapabilities capabilities)
    {
        string artifactName = Path.GetFileNameWithoutExtension(filePath);
        string format = Path.GetExtension(filePath).TrimStart('.').ToLowerInvariant();

        return new LMModel
        {
            Id = $"local:{artifactName}",
            Registry = "local",
            RepoId = artifactName,
            Name = artifactName,
            ArtifactName = artifactName,
            Format = format,
            Type = ModelType.TextGeneration, // Assume text generation by default
            Capabilities = capabilities,
            FilePaths = new List<string> { filePath },
            SizeInBytes = new FileInfo(filePath).Length,
            LocalPath = Path.GetDirectoryName(filePath)
        };
    }

    /// <summary>
    /// Creates a model from a repository and artifact
    /// </summary>
    public static LMModel FromRepositoryAndArtifact(LMRepo repo, ModelArtifact artifact)
    {
        var model = new LMModel
        {
            Registry = repo.Registry,
            RepoId = repo.RepoId,
            Name = $"{Path.GetFileName(repo.RepoId)} ({artifact.Name})",
            Description = artifact.Description,
            Version = repo.Version,
            Capabilities = repo.Capabilities.Clone(),
            ArtifactName = artifact.Name,
            Format = artifact.Format,
            SizeInBytes = artifact.SizeInBytes,
            FilePaths = artifact.FilePaths.ToList(),
            Type = repo.Type
        };

        // Generate the full ID in the format registry:repoId/artifactName
        model.Id = $"{repo.Registry}:{repo.RepoId}/{artifact.Name}";

        return model;
    }
}