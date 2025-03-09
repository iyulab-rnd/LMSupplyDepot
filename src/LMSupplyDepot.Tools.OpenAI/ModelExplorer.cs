namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Provides information about OpenAI models and features
/// </summary>
public class ModelExplorer
{
    private readonly OpenAIClient _chatClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelExplorer"/> class
    /// </summary>
        public ModelExplorer(OpenAIClient chatClient)
    {
        _chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
    }

    /// <summary>
    /// Lists all available models
    /// </summary>
    public async Task<ListModelsResponse> ListAllModelsAsync(CancellationToken cancellationToken = default)
    {
        return await _chatClient.ListModelsAsync(cancellationToken);
    }

    /// <summary>
    /// Gets models filtered by family
    /// </summary>
    public async Task<List<ModelInfo>> GetModelsByFamilyAsync(string family, CancellationToken cancellationToken = default)
    {
        var models = await _chatClient.ListModelsAsync(cancellationToken);
        return models.Data.Where(m => m.Family == family).ToList();
    }

    /// <summary>
    /// Gets GPT models
    /// </summary>
    public async Task<List<ModelInfo>> GetGptModelsAsync(CancellationToken cancellationToken = default)
    {
        return await GetModelsByFamilyAsync(ModelFamilies.GPT, cancellationToken);
    }

    /// <summary>
    /// Gets Reasoning models
    /// </summary>
    public async Task<List<ModelInfo>> GetReasoningModelsAsync(CancellationToken cancellationToken = default)
    {
        return await GetModelsByFamilyAsync(ModelFamilies.Reasoning, cancellationToken);
    }

    /// <summary>
    /// Gets Embeddings models
    /// </summary>
    public async Task<List<ModelInfo>> GetEmbeddingsModelsAsync(CancellationToken cancellationToken = default)
    {
        return await GetModelsByFamilyAsync(ModelFamilies.Embeddings, cancellationToken);
    }

    /// <summary>
    /// Gets detailed information about a specific model
    /// </summary>
    public async Task<ModelInfo> GetModelDetailsAsync(string modelId, CancellationToken cancellationToken = default)
    {
        return await _chatClient.RetrieveModelAsync(modelId, cancellationToken);
    }

    /// <summary>
    /// Suggests appropriate model based on task description
    /// </summary>
    public async Task<ModelInfo> SuggestModelForTaskAsync(string taskDescription, CancellationToken cancellationToken = default)
    {
        var prompt = $"I need to select an appropriate OpenAI model for the following task: {taskDescription}. " +
                     "Please recommend the most suitable model and explain why it's the best fit. " +
                     "Only recommend from the following model options: gpt-4o, gpt-4o-mini, o1, o1-mini, o3-mini, text-embedding-3-small, text-embedding-3-large.";

        var response = await _chatClient.CreateSimpleChatCompletionAsync("gpt-4o-mini", prompt, cancellationToken);
        var modelName = ExtractModelNameFromSuggestion(_chatClient.GetCompletionText(response));

        if (!string.IsNullOrEmpty(modelName))
        {
            try
            {
                return await _chatClient.RetrieveModelAsync(modelName, cancellationToken);
            }
            catch
            {
                // If there's an error retrieving the model, return default info
                return CreateDefaultModelInfo(modelName);
            }
        }

        // Default to gpt-4o if no model was successfully extracted
        return CreateDefaultModelInfo("gpt-4o");
    }

    /// <summary>
    /// Creates a default model info object for a model ID
    /// </summary>
    private ModelInfo CreateDefaultModelInfo(string modelId)
    {
        var modelInfo = new ModelInfo { Id = modelId, Name = modelId };

        if (modelId.StartsWith("gpt-4o"))
        {
            modelInfo.Family = ModelFamilies.GPT;
            modelInfo.ContextWindow = 128000;
            modelInfo.MaxOutputTokens = 16384;
            modelInfo.Description = "GPT-4o ('o' for 'omni') is a versatile, high-intelligence flagship model.";
        }
        else if (modelId.StartsWith('o'))
        {
            modelInfo.Family = ModelFamilies.Reasoning;
            modelInfo.ContextWindow = 200000;
            modelInfo.MaxOutputTokens = 100000;
            modelInfo.Description = "The o-series models are trained with reinforcement learning to perform complex reasoning.";
        }
        else if (modelId.Contains("embedding"))
        {
            modelInfo.Family = ModelFamilies.Embeddings;
            modelInfo.ContextWindow = 8191;
            modelInfo.Description = "Embedding models convert text into numerical form for use in search, clustering, and other tasks.";
        }

        return modelInfo;
    }

    /// <summary>
    /// Extracts a model name from a suggestion text
    /// </summary>
    private string ExtractModelNameFromSuggestion(string suggestionText)
    {
        var modelPatterns = new List<string>
        {
            "gpt-4o",
            "gpt-4o-mini",
            "o1",
            "o1-mini",
            "o3-mini",
            "text-embedding-3-small",
            "text-embedding-3-large"
        };

        foreach (var pattern in modelPatterns)
        {
            if (suggestionText.Contains(pattern))
            {
                return pattern;
            }
        }

        return null;
    }
}