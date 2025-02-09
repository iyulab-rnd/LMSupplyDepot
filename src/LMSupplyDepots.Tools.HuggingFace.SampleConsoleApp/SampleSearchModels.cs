using LMSupplyDepots.Tools.HuggingFace.Client;
using LMSupplyDepots.Tools.HuggingFace.Models;

namespace LMSupplyDepots.Tools.HuggingFace.SampleConsoleApp;

internal class SampleSearchModels
{
    public static async Task RunAsync(IHuggingFaceClient client)
    {
        Console.WriteLine("\nModel Search Sample");
        Console.WriteLine("------------------");

        Console.WriteLine("Select model type:");
        Console.WriteLine("1. Text Generation Models (default)");
        Console.WriteLine("2. Embedding Models");
        var typeChoice = Console.ReadLine()?.Trim();
        var isEmbedding = typeChoice == "2";

        Console.Write("Enter search term (or press Enter for all): ");
        var searchTerm = Console.ReadLine();

        Console.Write("Enter number of results (default 5): ");
        var limitStr = Console.ReadLine();
        var limit = string.IsNullOrWhiteSpace(limitStr) ? 5 : int.Parse(limitStr);

        Console.WriteLine("\nSelect sort field:");
        Console.WriteLine("1. Downloads (default)");
        Console.WriteLine("2. Likes");
        Console.WriteLine("3. Created Date");
        Console.WriteLine("4. Last Modified Date");

        var sortChoice = Console.ReadLine();
        var sortField = sortChoice switch
        {
            "2" => ModelSortField.Likes,
            "3" => ModelSortField.CreatedAt,
            "4" => ModelSortField.LastModified,
            _ => ModelSortField.Downloads
        };

        Console.WriteLine($"\nSearching {(isEmbedding ? "embedding" : "text generation")} models...");

        var models = isEmbedding
            ? await client.SearchEmbeddingModelsAsync(
                search: searchTerm,
                limit: limit,
                sortField: sortField,
                descending: true)
            : await client.SearchTextGenerationModelsAsync(
                search: searchTerm,
                limit: limit,
                sortField: sortField,
                descending: true);

        Console.WriteLine($"\nFound {models.Count} models (sorted by {sortField}):");
        foreach (var model in models)
        {
            Console.WriteLine($"\nId: {model.Id}");
            Console.WriteLine($"Model ID: {model.ModelId}");
            Console.WriteLine($"Author: {model.Author}");
            Console.WriteLine($"Downloads: {model.Downloads:N0}");
            Console.WriteLine($"Likes: {model.Likes:N0}");
            Console.WriteLine($"Last Modified: {model.LastModified:yyyy-MM-dd}");
            Console.WriteLine($"Files: {model.GetFilePaths().Length}");

            // Display model tags
            if (model.Tags.Any())
            {
                Console.WriteLine($"Tags: {string.Join(", ", model.Tags)}");
            }
        }
    }
}