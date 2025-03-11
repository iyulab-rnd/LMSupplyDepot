using LMSupplyDepot.Tools.OpenAI.Models;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace LMSupplyDepot.Tools.OpenAI.SampleConsoleApp.Chat;

public class StreamingExample
{
    private static string ApiKey;

    static async Task Main(string[] args)
    {
        Console.WriteLine("OpenAI Assistant Chat Sample");
        Console.WriteLine("============================");

        // Load API key
        LoadApiKey();

        await RunExample();
    }

    private static void LoadApiKey()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        ApiKey = configuration["OpenAI:ApiKey"];

        if (string.IsNullOrEmpty(ApiKey))
        {
            Console.WriteLine("API key not found. Please check your appsettings.json file.");
            Environment.Exit(1);
        }
    }

    public static async Task RunExample()
    {
        try
        {
            // Create the client
            var client = new OpenAIAssistantsClient(ApiKey);

            // Replace with your assistant ID
            string assistantId = "asst_js1S1rPP9YEVi4XewERnOlQl";

            // Create a thread with an initial message and prepare streaming
            var (threadId, streamHandler) = await client.CreateThreadWithMessageAndPrepareStreamingRunAsync(
                assistantId,
                "gpt-4 요약해줘"
            );

            Console.WriteLine($"Started streaming for thread: {threadId}");

            // Set up event handlers
            StringBuilder messageBuilder = new StringBuilder();

            // Handle events as they arrive
            streamHandler.OnData += (streamEvent) =>
            {
                // Process different event types
                switch (streamEvent.Event)
                {
                    case StreamEventTypes.ThreadMessageDelta:
                        var messageDelta = streamEvent.GetDataAs<MessageDelta>();

                        // If there's text content in the delta
                        var content = messageDelta?.Delta?.Content;
                        if (content != null && content.Count > 0)
                        {
                            foreach (var contentItem in content)
                            {
                                if (contentItem.Type == "text" && contentItem.Text?.Value != null)
                                {
                                    // Append the text to our message
                                    string text = contentItem.Text.Value;
                                    messageBuilder.Append(text);

                                    // Print the chunk as it arrives
                                    Console.Write(text);
                                }
                            }
                        }
                        break;

                    case StreamEventTypes.ThreadRunCompleted:
                        Console.WriteLine("\n[Run completed]");
                        break;

                    case "done":
                        Console.WriteLine("\n[Stream finished]");
                        break;

                    default:
                        // For debugging, you might want to see other events
                        Console.WriteLine($"\n[Event: {streamEvent.Event}]");
                        break;
                }
            };

            // Handle errors
            streamHandler.OnError += (ex) =>
            {
                Console.WriteLine($"\nError: {ex.Message}");
            };

            // Handle completion
            streamHandler.OnComplete += () =>
            {
                Console.WriteLine("\nStreaming completed");
                Console.WriteLine($"Complete message: {messageBuilder}");
            };

            // Start the streaming process
            await streamHandler.StartStreamingAsync();

            // Wait for streaming to complete
            Console.WriteLine("Press any key to stop streaming...");
            Console.ReadKey();

            // Clean up
            streamHandler.StopStreaming();
            streamHandler.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner error: {ex.InnerException.Message}");
            }
        }
    }
}