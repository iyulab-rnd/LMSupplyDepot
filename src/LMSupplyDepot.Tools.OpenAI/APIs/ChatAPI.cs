using OpenAI;
using OpenAI.Responses;
using System.Text;

namespace LMSupplyDepot.Tools.OpenAI.APIs;

/// <summary>
/// Manages chat operations with OpenAI
/// </summary>
public class ChatAPI
{
    private readonly OpenAIResponseClient _responseClient;

    /// <summary>
    /// Initializes a new instance of the ChatAPI class
    /// </summary>
    /// <param name="client">The OpenAI client</param>
    /// <param name="model">The OpenAI model to use for chat</param>
    public ChatAPI(OpenAIClient client, string model = "gpt-4o")
    {
        _responseClient = client.GetOpenAIResponseClient(model);
    }

    /// <summary>
    /// Sends a single message to the model and returns the response
    /// </summary>
    /// <param name="message">The user's message</param>
    /// <param name="systemPrompt">Optional system prompt to set context</param>
    /// <returns>The model's response</returns>
    public async Task<string> SendMessageAsync(string message, string systemPrompt = null)
    {
        Console.WriteLine($"Sending message to OpenAI: \"{message}\"");

        try
        {
            // Create input items for the conversation
            var inputItems = new List<ResponseItem>();

            // Add system message if provided
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                inputItems.Add(ResponseItem.CreateSystemMessageItem(systemPrompt));
            }

            // Add user message
            inputItems.Add(ResponseItem.CreateUserMessageItem(message));

            // Send the request
            var response = await _responseClient.CreateResponseAsync(
                inputItems,
                new ResponseCreationOptions());

            // Extract the response text
            string responseText = response.Value.GetOutputText();
            Console.WriteLine($"Response received from OpenAI");

            return responseText;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Conducts a conversation with the model using a list of messages
    /// </summary>
    /// <param name="messages">List of message tuples (role, content)</param>
    /// <returns>The model's response</returns>
    public async Task<string> SendConversationAsync(List<(string role, string content)> messages)
    {
        Console.WriteLine($"Sending conversation with {messages.Count} messages to OpenAI");

        try
        {
            // Convert messages to ResponseItems
            var inputItems = new List<ResponseItem>();

            foreach (var (role, content) in messages)
            {
                switch (role.ToLower())
                {
                    case "system":
                        inputItems.Add(ResponseItem.CreateSystemMessageItem(content));
                        break;
                    case "assistant":
                        inputItems.Add(ResponseItem.CreateAssistantMessageItem(content));
                        break;
                    case "user":
                    default:
                        inputItems.Add(ResponseItem.CreateUserMessageItem(content));
                        break;
                }
            }

            // Send the request
            var response = await _responseClient.CreateResponseAsync(
                inputItems,
                new ResponseCreationOptions());

            // Extract the response text
            string responseText = response.Value.GetOutputText();
            Console.WriteLine($"Response received from OpenAI");

            return responseText;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending conversation: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Streams a chat response from the model
    /// </summary>
    /// <param name="message">The user's message</param>
    /// <param name="systemPrompt">Optional system prompt to set context</param>
    /// <param name="onUpdate">Action to handle each chunk of the streaming response</param>
    /// <returns>The complete response text</returns>
    public async Task<string> StreamMessageAsync(string message, string systemPrompt = null, Action<string> onUpdate = null)
    {
        Console.WriteLine($"Streaming message to OpenAI: \"{message}\"");

        try
        {
            // Create input items for the conversation
            var inputItems = new List<ResponseItem>();

            // Add system message if provided
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                inputItems.Add(ResponseItem.CreateSystemMessageItem(systemPrompt));
            }

            // Add user message
            inputItems.Add(ResponseItem.CreateUserMessageItem(message));

            // Create the streaming request
            var streamingResponse = _responseClient.CreateResponseStreamingAsync(
                inputItems,
                new ResponseCreationOptions());

            StringBuilder fullResponse = new StringBuilder();

            // Process each chunk as it arrives
            await foreach (var update in streamingResponse)
            {
                // Process different update types based on the Type property
                switch (update)
                {
                    case StreamingResponseOutputTextDeltaUpdate textDelta:
                        // Handle text delta updates
                        string chunk = textDelta.Delta;
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            fullResponse.Append(chunk);
                            onUpdate?.Invoke(chunk);
                        }
                        break;

                        // Add cases for other update types as needed
                        // For example:
                        // case StreamingResponseContentPartAddedUpdate contentPartAdded:
                        //     // Handle content part added updates
                        //     break;
                }
            }

            string completeResponse = fullResponse.ToString();
            Console.WriteLine($"Streaming response completed");

            return completeResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error streaming message: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Streams a conversation with the model using a list of messages
    /// </summary>
    /// <param name="messages">List of message tuples (role, content)</param>
    /// <param name="onUpdate">Action to handle each chunk of the streaming response</param>
    /// <returns>The complete response text</returns>
    public async Task<string> StreamConversationAsync(List<(string role, string content)> messages, Action<string> onUpdate = null)
    {
        Console.WriteLine($"Streaming conversation with {messages.Count} messages to OpenAI");

        try
        {
            // Convert messages to ResponseItems
            var inputItems = new List<ResponseItem>();

            foreach (var (role, content) in messages)
            {
                switch (role.ToLower())
                {
                    case "system":
                        inputItems.Add(ResponseItem.CreateSystemMessageItem(content));
                        break;
                    case "assistant":
                        inputItems.Add(ResponseItem.CreateAssistantMessageItem(content));
                        break;
                    case "user":
                    default:
                        inputItems.Add(ResponseItem.CreateUserMessageItem(content));
                        break;
                }
            }

            // Create the streaming request
            var streamingResponse = _responseClient.CreateResponseStreamingAsync(
                inputItems,
                new ResponseCreationOptions());

            StringBuilder fullResponse = new StringBuilder();

            // Process each chunk as it arrives
            await foreach (var update in streamingResponse)
            {
                // Process different update types based on the Type property
                switch (update)
                {
                    case StreamingResponseOutputTextDeltaUpdate textDelta:
                        // Handle text delta updates
                        string chunk = textDelta.Delta;
                        if (!string.IsNullOrEmpty(chunk))
                        {
                            fullResponse.Append(chunk);
                            onUpdate?.Invoke(chunk);
                        }
                        break;

                        // Add cases for other update types as needed
                }
            }

            string completeResponse = fullResponse.ToString();
            Console.WriteLine($"Streaming response completed");

            return completeResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error streaming conversation: {ex.Message}");
            throw;
        }
    }
}