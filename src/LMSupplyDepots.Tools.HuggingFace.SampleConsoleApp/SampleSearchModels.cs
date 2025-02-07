using LMSupplyDepots.Tools.HuggingFace.Client;
using LMSupplyDepots.Tools.HuggingFace.Models;

namespace LMSupplyDepots.Tools.HuggingFace.SampleConsoleApp;

internal class SampleSearchModels
{
    public static async Task RunAsync(IHuggingFaceClient client)
    {
        Console.WriteLine("\nModel Search Sample");
        Console.WriteLine("------------------");

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

        Console.WriteLine("\nSearching models...");
        var models = await client.SearchModelsAsync(
            search: searchTerm,
            limit: limit,
            sortField: sortField,
            descending: true);

        Console.WriteLine($"\nFound {models.Count} models (sorted by {sortField}):");
        foreach (var model in models)
        {
            Console.WriteLine($"\nID: {model.ID}");
            Console.WriteLine($"Author: {model.Author}");
            Console.WriteLine($"Downloads: {model.Downloads:N0}");
            Console.WriteLine($"Likes: {model.Likes:N0}");
            Console.WriteLine($"Created: {model.CreatedAt:yyyy-MM-dd}");
            Console.WriteLine($"Files: {model.GetFilePaths().Length}");
        }
    }
}