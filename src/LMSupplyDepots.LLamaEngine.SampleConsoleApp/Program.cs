using LMSupplyDepots.LLamaEngine;
using LMSupplyDepots.LLamaEngine.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var modelPath = @"D:\filer-data\models\text-generation\MaziyarPanahi\Llama-3.2-1B-Instruct-GGUF\Llama-3.2-1B-Instruct.fp16.gguf";
var modelIdentifier = "MaziyarPanahi/Llama-3.2-1B-Instruct:Llama-3.2-1B-Instruct.fp16.gguf";

// DI 설정
var services = new ServiceCollection();
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
services.AddLLamaEngine();

using var serviceProvider = services.BuildServiceProvider();
var modelManager = serviceProvider.GetRequiredService<ILocalModelManager>();
var llmService = serviceProvider.GetRequiredService<ILocalLLMService>();

// 모델 로드
Console.WriteLine("Loading LLM model...");
var modelInfo = await modelManager.LoadModelAsync(modelPath, modelIdentifier);
if (modelInfo?.State != LMSupplyDepots.LLamaEngine.Models.LocalModelState.Loaded)
{
    Console.WriteLine($"Failed to load model: {modelInfo?.LastError ?? "Unknown error"}");
    return;
}

var inferenceParams = ParameterFactory.NewInferenceParams();

// Interactive chat loop
Console.WriteLine("\nChat started. Type 'exit' to quit.");
Console.WriteLine("Type 'save' to save the chat state.");
Console.WriteLine("Type 'regenerate' to regenerate the last response.");
Console.WriteLine("-------------------");

string systemPrompt = @"You are a helpful AI assistant that speaks Korean.
Provide clear and helpful answers, but be honest about what you don't know.
Your answers are always in Korean.</s>";

try
{
    // 시스템 프롬프트로 초기화
    await llmService.InferAsync(modelIdentifier, systemPrompt, inferenceParams);

    while (true)
    {
        Console.Write("\nUser: ");
        var input = Console.ReadLine();

        if (string.IsNullOrEmpty(input) || input.ToLower() == "exit")
            break;

        Console.Write("Assistant: ");

        try
        {
            await foreach (var text in llmService.InferStreamAsync(
                modelIdentifier,
                input,
                inferenceParams))
            {
                Console.Write(text);
            }
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError occurred during inference: {ex.Message}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"\nError occurred: {ex.Message}");
}
finally
{
    // 모델 언로드
    await modelManager.UnloadModelAsync(modelIdentifier);
}

Console.WriteLine("\nChat ended. Press any key to exit.");
Console.ReadKey();