using LLama.Common;
using LLama.Sampling;

namespace LMSupplyDepots.LLamaEngine;

public static class ParameterFactory
{
    public static InferenceParams NewInferenceParams(
        int maxTokens = 2048,
        IEnumerable<string>? antiprompt = null,
        float temperature = 0.7f,
        float topP = 0.9f,
        float repeatPenalty = 1.1f)
    {
        antiprompt ??= ["User:", "Assistant:", "\n\n"];

        // 추론 파라미터 설정
        var inferenceParams = new InferenceParams
        {
            MaxTokens = maxTokens,
            AntiPrompts = antiprompt.ToList(),
            SamplingPipeline = new DefaultSamplingPipeline
            {
                Temperature = temperature,
                TopP = topP,
                RepeatPenalty = repeatPenalty
            }
        };
        return inferenceParams;
    }
}