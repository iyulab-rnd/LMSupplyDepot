namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Extension methods for ModelExplorer to improve model suggestion
/// </summary>
public partial class ModelExplorer
{
    /// <summary>
    /// Represents a model suggestion result
    /// </summary>
    public class ModelSuggestionResult
    {
        /// <summary>
        /// The suggested model info
        /// </summary>
        public ModelInfo ModelInfo { get; set; }

        /// <summary>
        /// The explanation for why this model was suggested
        /// </summary>
        public string Explanation { get; set; }
    }

    /// <summary>
    /// Suggests appropriate model based on task description with explanation
    /// </summary>
    public async Task<ModelSuggestionResult> SuggestModelForTaskWithExplanationAsync(string taskDescription, CancellationToken cancellationToken = default)
    {
        var prompt = $"I need to select an appropriate OpenAI model for the following task: {taskDescription}. " +
                     "Please recommend the most suitable model and explain why it's the best fit. " +
                     "Return your response in this JSON format: {\"model\":\"model_name\",\"explanation\":\"detailed explanation\"} " +
                     "Only recommend from the following model options: gpt-4o, gpt-4o-mini, o1, o1-mini, o3-mini, text-embedding-3-small, text-embedding-3-large.";

        var response = await _client.CreateSimpleChatCompletionAsync("gpt-4o-mini", prompt, cancellationToken);
        var completionText = _client.GetCompletionText(response);

        try
        {
            // Parse JSON response
            var suggestionJson = JsonDocument.Parse(completionText);
            var root = suggestionJson.RootElement;

            string modelName = root.GetProperty("model").GetString();
            string explanation = root.GetProperty("explanation").GetString();

            ModelInfo modelInfo;
            try
            {
                modelInfo = await _client.RetrieveModelAsync(modelName, cancellationToken);
            }
            catch
            {
                // If there's an error retrieving the model, return default info
                modelInfo = CreateDefaultModelInfo(modelName);
            }

            return new ModelSuggestionResult
            {
                ModelInfo = modelInfo,
                Explanation = explanation
            };
        }
        catch
        {
            // If JSON parsing fails, fall back to simpler approach
            var modelName = ExtractModelNameFromSuggestion(completionText);
            if (string.IsNullOrEmpty(modelName))
            {
                modelName = "gpt-4o"; // Default to gpt-4o if extraction fails
            }

            ModelInfo modelInfo;
            try
            {
                modelInfo = await _client.RetrieveModelAsync(modelName, cancellationToken);
            }
            catch
            {
                // If there's an error retrieving the model, return default info
                modelInfo = CreateDefaultModelInfo(modelName);
            }

            return new ModelSuggestionResult
            {
                ModelInfo = modelInfo,
                Explanation = "This model was suggested based on your task requirements."
            };
        }
    }
}