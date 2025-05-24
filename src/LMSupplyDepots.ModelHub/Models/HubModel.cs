using LMSupplyDepots.External.HuggingFace.Models;

namespace LMSupplyDepots.ModelHub.Models;

/// <summary>
/// Extended model representation for ModelHub with repository and artifact information
/// </summary>
public class HubModel
{
    /// <summary>
    /// The core model information
    /// </summary>
    public LMModel Model { get; set; } = new();

    /// <summary>
    /// Reference to the repository this model belongs to
    /// </summary>
    public LMRepo Repository { get; set; }

    /// <summary>
    /// Model artifact specification this model represents
    /// </summary>
    public ModelArtifact Artifact { get; set; }

    /// <summary>
    /// Creates a new HubModel instance
    /// </summary>
    public HubModel(LMModel model, LMRepo repository, ModelArtifact artifact)
    {
        Model = model;
        Repository = repository;
        Artifact = artifact;
    }

    /// <summary>
    /// Creates a HubModel from a repository and artifact
    /// </summary>
    public static HubModel FromRepositoryAndArtifact(LMRepo repo, ModelArtifact artifact)
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

        return new HubModel(model, repo, artifact);
    }

    /// <summary>
    /// Converts to a standard LMModel
    /// </summary>
    public LMModel ToLMModel()
    {
        // Ensure the model has all the necessary properties updated from Repository and Artifact
        Model.Registry = Repository.Registry;
        Model.RepoId = Repository.RepoId;
        Model.Type = Repository.Type;
        Model.Format = Artifact.Format;
        Model.ArtifactName = Artifact.Name;
        Model.SizeInBytes = Artifact.SizeInBytes;
        Model.FilePaths = Artifact.FilePaths.ToList();

        // Ensure ID is in the correct format
        if (string.IsNullOrEmpty(Model.Id) || !Model.Id.Contains(':') || !Model.Id.Contains('/'))
        {
            Model.Id = $"{Repository.Registry}:{Repository.RepoId}/{Artifact.Name}";
        }

        return Model;
    }
}