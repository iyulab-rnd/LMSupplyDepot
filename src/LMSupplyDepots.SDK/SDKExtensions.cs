using LMSupplyDepots.Interfaces;
using LMSupplyDepots.ModelHub.Interfaces;

namespace LMSupplyDepots.SDK;

/// <summary>
/// Utility class for resolving model keys (IDs or aliases) to actual model IDs
/// </summary>
internal static class SDKExtensions
{
    /// <summary>
    /// Resolves a model key (ID or alias) to the actual model ID
    /// </summary>
    public static async Task<string> ResolveModelKeyAsync(
        this IModelManager modelManager,
        string modelKey,
        CancellationToken cancellationToken = default)
    {
        // First, try to find the model directly by the provided key (as ID)
        var model = await modelManager.GetModelAsync(modelKey, cancellationToken);
        if (model != null)
        {
            return model.Id;
        }

        // If not found by ID, check if we can find a model with this alias
        model = await modelManager.GetModelByAliasAsync(modelKey, cancellationToken);
        if (model != null)
        {
            return model.Id;
        }

        // If we get here, we couldn't resolve the model, so just return the original key
        return modelKey;
    }

    /// <summary>
    /// Resolves a model key (which could be an alias or an ID) to the actual model ID
    /// </summary>
    public static async Task<string> ResolveModelKeyAsync(
        this IModelRepository repository,
        string modelKey,
        CancellationToken cancellationToken = default)
    {
        // First, try to find the model directly by the provided key (as ID)
        var model = await repository.GetModelAsync(modelKey, cancellationToken);
        if (model != null)
        {
            return model.Id;
        }

        // If not found by ID, check if we can find a model with this alias
        var models = await repository.ListModelsAsync(
            null, null, 0, int.MaxValue, cancellationToken);

        var matchedModel = models.FirstOrDefault(m =>
            !string.IsNullOrEmpty(m.Alias) &&
            string.Equals(m.Alias, modelKey, StringComparison.OrdinalIgnoreCase));

        if (matchedModel != null)
        {
            return matchedModel.Id;
        }

        // If we get here, we couldn't resolve the model, so just return the original key
        return modelKey;
    }
}