# LMSupplyDepot.Tools.OpenAI

C# ���̺귯���� OpenAI�� Assistants API, Chat API �� Vector Store API�� ���� ������ �� �ֽ��ϴ�.

## Ư¡

- OpenAI Assistants API ��ü ��� ����
- Chat Completions API ����
- �Ӻ��� API ����
- Vector Store API ����
- �� Ž�� �� �� ��õ ���
- ������ �� ����� API ���濡 ���� ����
- ���� ���� �޼���� ������ ���� ����
- ���� �Ӽ� ���� ������� Ȯ�强 ����

## ��ġ

NuGet ��Ű�� �����ڸ� ���� ��ġ�� �� �ֽ��ϴ�:

```bash
dotnet add package LMSupplyDepot.Tools.OpenAI
```

## ���� ����

### Chat Completions API ����ϱ�

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // API Ű�� Ŭ���̾�Ʈ �ʱ�ȭ
        var chatClient = new OpenAIClient("your-api-key-here");

        // ������ ��û���� ���� ��������
        var completion = await chatClient.CreateSimpleChatCompletionAsync(
            "gpt-4o",
            "3x + 11 = 14��� �������� Ǯ���� �� �������?"
        );

        // ���� ���
        Console.WriteLine(chatClient.GetCompletionText(completion));
    }
}
```

### �Լ� ȣ��(Function Calling) ����ϱ�

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var chatClient = new OpenAIClient("your-api-key-here");

        // �Լ� ��Ű�� ����
        var weatherFunctionParams = new
        {
            type = "object",
            properties = new
            {
                location = new
                {
                    type = "string",
                    description = "���� �̸�(��: ����, �λ�)"
                },
                unit = new
                {
                    type = "string",
                    enum = new[] { "celsius", "fahrenheit" },
                    description = "�µ� ����"
                }
            },
            required = new[] { "location" }
        };

        // �Լ� ������ ������ ��û ����
        var request = CreateChatCompletionRequest.Create(
            "gpt-4o",
            new List<ChatMessage> { ChatMessage.FromUser("������ ������ ��� �˷��ּ���.") }
        ).WithFunctionTool(
            "get_weather",
            "Ư�� ��ġ�� ���� ���� ������ �����ɴϴ�.",
            weatherFunctionParams
        );

        // ��û ������
        var completion = await chatClient.CreateChatCompletionAsync(request);

        // �Լ� ȣ�� Ȯ��
        if (chatClient.HasToolCalls(completion))
        {
            var toolCalls = chatClient.GetToolCalls(completion);
            foreach (var toolCall in toolCalls)
            {
                if (toolCall.Type == "function" && toolCall.GetFunctionName() == "get_weather")
                {
                    // �Լ� ���� �Ľ�
                    var args = toolCall.GetFunctionArguments();
                    Console.WriteLine($"�Լ� ȣ��: {toolCall.GetFunctionName()}, ����: {args}");

                    // ���⼭ ���� ���� ������ �������� ���� ����
                    // ...

                    // �Լ� ���� ����� ��ȭ ����ϱ�
                    var toolOutputs = new List<ToolOutput>
                    {
                        ToolOutput.Create(
                            toolCall.Id,
                            JsonSerializer.Serialize(new
                            {
                                location = "����",
                                temperature = 22.5,
                                unit = "celsius",
                                condition = "����"
                            })
                        )
                    };

                    // �Լ� ȣ�� ����� ��ȭ ���
                    var followUpCompletion = await chatClient.ContinueWithToolOutputsAsync(
                        "gpt-4o",
                        completion.Choices[0].Message,
                        toolOutputs
                    );

                    Console.WriteLine(chatClient.GetCompletionText(followUpCompletion));
                }
            }
        }
    }
}
```

### ��ȭ ��� �����

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using LMSupplyDepot.Tools.OpenAI.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var chatClient = new OpenAIClient("your-api-key-here");

        // ��ȭ ��� �����
        var messages = ChatClientHelpers.BuildConversationHistory(
            "�ȳ��ϼ���!", // ���� �޽���
            "�ȳ��ϼ���! ������ ���͵帱���?", // ��ý���Ʈ �޽���
            "GPT-4�� GPT-4o�� �������� ������?" // ���� �޽���
        );

        // ������(�ý���) �޽��� �߰�
        messages.Insert(0, ChatMessage.FromDeveloper("����� OpenAI �𵨿� ���� �����ϴ� �������Դϴ�."));

        // ä�� �ϼ� ��û ����
        var request = CreateChatCompletionRequest.Create("gpt-4o", messages)
            .WithTemperature(0.7)
            .WithMaxTokens(500);

        // ��û ������
        var completion = await chatClient.CreateChatCompletionAsync(request);

        // ���� ���
        Console.WriteLine(chatClient.GetCompletionText(completion));
    }
}
```

### �̹��� �Է��� �ִ� ��Ƽ��� �޽���

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using LMSupplyDepot.Tools.OpenAI.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var chatClient = new OpenAIClient("your-api-key-here");

        // ��Ƽ��� �޽��� ���� (�ؽ�Ʈ�� �̹��� ���ÿ� ����)
        var multimodalMessage = ChatClientHelpers.CreateMultimodalMessage(
            "�� �̹����� ������ �ֳ���?",
            "https://example.com/image.jpg"
        );

        // ä�� �ϼ� ��û ����
        var request = CreateChatCompletionRequest.Create(
            "gpt-4o",
            new List<ChatMessage> { multimodalMessage }
        );

        // ��û ������
        var completion = await chatClient.CreateChatCompletionAsync(request);

        // ���� ���
        Console.WriteLine(chatClient.GetCompletionText(completion));
    }
}
```

### JSON ���� ���� �ޱ�

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using LMSupplyDepot.Tools.OpenAI.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var chatClient = new OpenAIClient("your-api-key-here");

        // JSON ��Ű�� ����
        var schema = new
        {
            type = "object",
            properties = new
            {
                name = new { type = "string" },
                age = new { type = "integer" },
                email = new { type = "string" }
            },
            required = new[] { "name", "email" }
        };

        // ä�� �ϼ� ��û ����
        var request = CreateChatCompletionRequest.Create(
            "gpt-4o",
            new List<ChatMessage> { ChatMessage.FromUser("�̸��� ȫ�浿, ���̴� 30��, �̸����� hong@example.com�� ����� ������ JSON���� �������ּ���.") }
        ).WithResponseFormat(ChatClientHelpers.CreateJsonResponseFormat(schema));

        // ��û ������
        var completion = await chatClient.CreateChatCompletionAsync(request);

        // ���� ���
        Console.WriteLine(chatClient.GetCompletionText(completion));
    }
}
```

### �Ӻ��� �����ϱ�

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using LMSupplyDepot.Tools.OpenAI.Utilities;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var chatClient = new OpenAIClient("your-api-key-here");

        // �ؽ�Ʈ �Ӻ��� ����
        var embeddingsResponse = await chatClient.CreateEmbeddingAsync(
            "text-embedding-3-small",
            "OpenAI�� �Ӻ��� API�� �ؽ�Ʈ�� ��ġ ���ͷ� ��ȯ�մϴ�."
        );

        // �Ӻ��� ���� ��������
        var embeddingVector = chatClient.GetEmbeddingVector(embeddingsResponse);
        
        Console.WriteLine($"�Ӻ��� ���� ��: {embeddingVector.Count}");
        
        // ���� �� �����Ͽ� �Ӻ��� ���� (ª�� �Ӻ���)
        var shorterEmbeddingsResponse = await chatClient.CreateEmbeddingAsync(
            "text-embedding-3-small",
            "OpenAI�� �Ӻ��� API�� �ؽ�Ʈ�� ��ġ ���ͷ� ��ȯ�մϴ�.",
            dimensions: 256
        );
        
        var shorterVector = chatClient.GetEmbeddingVector(shorterEmbeddingsResponse);
        Console.WriteLine($"��ҵ� �Ӻ��� ���� ��: {shorterVector.Count}");
        
        // �Ӻ������� �ǹ��� ���絵 ���
        var secondEmbeddingsResponse = await chatClient.CreateEmbeddingAsync(
            "text-embedding-3-small",
            "�ؽ�Ʈ�� ��ġ�� ǥ������ ��ȯ�ϴ� OpenAI�� ����Դϴ�."
        );
        
        var secondVector = chatClient.GetEmbeddingVector(secondEmbeddingsResponse);
        
        double similarity = ChatClientHelpers.CosineSimilarity(embeddingVector, secondVector);
        Console.WriteLine($"�ؽ�Ʈ ���絵: {similarity}");
    }
}
```

### �� ���� Ž���ϱ�

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var chatClient = new OpenAIClient("your-api-key-here");
        var modelExplorer = new ModelExplorer(chatClient);

        // ��� �� ��� ��������
        var models = await modelExplorer.ListAllModelsAsync();
        Console.WriteLine($"��� ������ �� ��: {models.Data.Count}");

        // GPT �𵨸� ��������
        var gptModels = await modelExplorer.GetGptModelsAsync();
        Console.WriteLine("\nGPT ��:");
        foreach (var model in gptModels)
        {
            Console.WriteLine($"- {model.Id}: {model.Description}");
        }

        // �Ӻ��� �𵨸� ��������
        var embeddingModels = await modelExplorer.GetEmbeddingsModelsAsync();
        Console.WriteLine("\n�Ӻ��� ��:");
        foreach (var model in embeddingModels)
        {
            Console.WriteLine($"- {model.Id}: {model.Description}");
        }

        // �۾��� �´� �� ��õ �ޱ�
        var suggestedModel = await modelExplorer.SuggestModelForTaskAsync(
            "�ѱ���� ��� ȥ�յ� �� ���� �����Ϳ��� ���� �м��� �����ؾ� �մϴ�."
        );

        Console.WriteLine($"\n��õ ��: {suggestedModel.Id}");
        Console.WriteLine($"�� ����: {suggestedModel.Description}");
        Console.WriteLine($"���ؽ�Ʈ ������: {suggestedModel.ContextWindow} ��ū");
    }
}
```

### Assistants API ����ϱ�

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using LMSupplyDepot.Tools.OpenAI.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // API Ű�� Ŭ���̾�Ʈ �ʱ�ȭ
        var assistantsClient = new OpenAIAssistantsClient("your-api-key-here");

        // Assistant ����
        var assistant = await assistantsClient.CreateAssistantAsync(
            CreateAssistantRequest.Create("gpt-4o")
                .WithName("Math Tutor")
                .WithInstructions("���� ������ �ذ��ϴ� �� ������ �ִ� Ʃ���Դϴ�.")
                .WithTools(new List<Tool> { Tool.CreateCodeInterpreterTool() })
        );

        Console.WriteLine($"Assistant created with ID: {assistant.Id}");

        // Thread ���� �� �޽��� �߰�
        var thread = await assistantsClient.CreateThreadAsync();
        
        // ����� �޽��� �߰�
        await assistantsClient.CreateUserMessageAsync(thread.Id, "3x + 11 = 14��� �������� Ǯ���� �� �������?");
        
        // Run ���� �� ����
        var run = await assistantsClient.CreateSimpleRunAsync(thread.Id, assistant.Id);
        
        // Run�� �Ϸ�� ������ ���
        run = await assistantsClient.WaitForRunCompletionAsync(thread.Id, run.Id);
        
        // ���� �޽��� ��������
        var messages = await assistantsClient.ListMessagesAsync(thread.Id, order: "desc", limit: 1);
        var assistantMessage = messages.Data[0];
        
        // ���� ���
        Console.WriteLine("Assistant's response:");
        Console.WriteLine(AssistantHelpers.GetMessageText(assistantMessage));
    }
}
```

### ������ ��ȭ �����ϱ�

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using LMSupplyDepot.Tools.OpenAI.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var assistantsClient = new OpenAIAssistantsClient("your-api-key-here");

        // ������ ��ȭ ���� (Assistant ID�� �ʿ�)
        var assistantMessage = await assistantsClient.GetSimpleResponseAsync(
            "asst_123456789", // ���� Assistant ID�� ��ü
            "��� ������ ��� ����� �������ּ���."
        );

        // ���� ���
        Console.WriteLine(AssistantHelpers.GetMessageText(assistantMessage));

        // �Ǵ� ǳ���� ��ȭ ��� ��������
        var messages = await AssistantHelpers.RunConversationAsync(
            assistantsClient,
            "asst_123456789", // ���� Assistant ID�� ��ü
            new List<string> {
                "����� �����ΰ���?",
                "2x2 ����� ���� ����ּ���.",
                "�� ����� ��Ľ��� ����ϴ� �����?"
            }
        );

        // ��ü ��ȭ ���
        foreach (var msg in messages)
        {
            Console.WriteLine($"{msg.Role}: {AssistantHelpers.GetMessageText(msg)}");
            Console.WriteLine("---");
        }
    }
}
```

### Vector Store API ����ϱ�

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Vector Store Ŭ���̾�Ʈ �ʱ�ȭ
        var vectorStoreClient = new VectorStoreClient("your-api-key-here");
        
        // Vector Store ����
        var vectorStore = await vectorStoreClient.CreateVectorStoreAsync("My Knowledge Base");
        Console.WriteLine($"Vector Store created with ID: {vectorStore.Id}");
        
        // ���� ���ε� �� Vector Store�� �߰� (OpenAIAssistantsClient �ʿ�)
        var assistantsClient = new OpenAIAssistantsClient("your-api-key-here");
        
        // ���� ���� ���ε� �� �߰�
        var vectorStoreFile = await vectorStoreClient.UploadAndAddFileAsync(
            vectorStore.Id,
            "path/to/document.pdf",
            assistantsClient
        );
        
        Console.WriteLine($"File added to Vector Store with ID: {vectorStoreFile.Id}");
        
        // ���� ���� ���ε� �� �� Vector Store ����
        var filePaths = new List<string> { 
            "path/to/document1.pdf", 
            "path/to/document2.pdf", 
            "path/to/document3.docx" 
        };
        
        var newVectorStore = await vectorStoreClient.UploadFilesAndCreateVectorStoreAsync(
            "Documents Collection",
            filePaths,
            assistantsClient
        );
        
        Console.WriteLine($"New Vector Store with multiple files created: {newVectorStore.Id}");
        Console.WriteLine($"Total files: {newVectorStore.GetFileCounts().Total}");
        
        // Vector Store ��� ��������
        var vectorStores = await vectorStoreClient.ListVectorStoresAsync(limit: 10);
        
        Console.WriteLine("\nAvailable Vector Stores:");
        foreach (var store in vectorStores.Data)
        {
            Console.WriteLine($"- {store.Name} (ID: {store.Id}, Status: {store.Status})");
        }
        
        // �ϰ� ���� ���ε� (File Batch)
        var fileIds = new List<string> { "file-abc123", "file-def456" }; // �̹� ���ε�� ���� ID
        var batch = await vectorStoreClient.CreateVectorStoreFileBatchAsync(vectorStore.Id, fileIds);
        
        // ��ġ ó���� �Ϸ�� ������ ���
        batch = await vectorStoreClient.PollVectorStoreFileBatchUntilCompletedAsync(
            vectorStore.Id, 
            batch.Id
        );
        
        Console.WriteLine($"Batch processing completed: {batch.FileCounts.Completed} of {batch.FileCounts.Total} files processed");
        
        // Vector Store�� ����ϴ� Assistant ����
        assistant = await assistantsClient.CreateAssistantWithFileSearchAsync(
            "gpt-4o", 
            "���� �˻��� Ưȭ�� ��ý���Ʈ�Դϴ�.", 
            new List<string> { vectorStore.Id },
            "Document Expert"
        );
        
        Console.WriteLine($"Created an assistant with Vector Store: {assistant.Id}");
    }
}
```

### Vector Store�� �Բ� File Search ��� ����ϱ�

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var assistantsClient = new OpenAIAssistantsClient("your-api-key-here");
        var vectorStoreClient = new VectorStoreClient("your-api-key-here");
        
        // 1. Vector Store ���� �� ���� ���ε�
        var vectorStore = await vectorStoreClient.CreateVectorStoreAsync("Research Papers");
        
        // ���� ���ε� (�̹� ���ε�� ���� ID ���)
        await vectorStoreClient.CreateVectorStoreFileAsync(
            vectorStore.Id, 
            "file-abc123" // �̹� ���ε�� ���� ID
        );
        
        // 2. File Search ������ ���� Assistant ����
        var assistant = await assistantsClient.CreateAssistantAsync(
            CreateAssistantRequest.Create("gpt-4o")
                .WithName("Research Assistant")
                .WithInstructions("���� ������ ������ ã�� ������ִ� ��ý���Ʈ�Դϴ�.")
                .WithTools(new List<Tool> { Tool.CreateFileSearchTool() })
        );
        
        // 3. Vector Store ���ҽ� �߰�
        await assistantsClient.UpdateAssistantWithVectorStoresAsync(
            assistant.Id,
            new List<string> { vectorStore.Id }
        );
        
        // 4. Thread ���� �� �����ϱ�
        var thread = await assistantsClient.CreateThreadWithVectorStoresAsync(
            new List<string> { vectorStore.Id }
        );
        
        // 5. ���� �߰�
        await assistantsClient.CreateUserMessageAsync(
            thread.Id,
            "������ ������ ���� ���� �� ��ǥ�� ���� ������ ã���ּ���."
        );
        
        // 6. ����
        var run = await assistantsClient.CreateSimpleRunAsync(thread.Id, assistant.Id);
        run = await assistantsClient.WaitForRunCompletionAsync(thread.Id, run.Id);
        
        // 7. ���� Ȯ�� (���� �ο� ����)
        var messages = await assistantsClient.ListMessagesAsync(thread.Id, order: "desc", limit: 1);
        var responseWithCitations = await AssistantHelpers.GetMessageWithCitationsAsync(
            messages.Data[0],
            assistantsClient
        );
        
        Console.WriteLine("Response with citations:");
        Console.WriteLine(responseWithCitations);
    }
}
```

### Chunking ������ ����� Vector Store ���� ó��

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var vectorStoreClient = new VectorStoreClient("your-api-key-here");
        var assistantsClient = new OpenAIAssistantsClient("your-api-key-here");
        
        // 1. Vector Store ����
        var vectorStore = await vectorStoreClient.CreateVectorStoreAsync("Technical Documentation");
        
        // 2. ���� ûŷ �������� ���� ���ε�
        var fileId = "file-abc123"; // �̹� ���ε�� ���� ID
        
        var request = CreateVectorStoreFileRequest.Create(fileId)
            .WithStaticChunkingStrategy(
                maxChunkSizeTokens: 1000,  // �ִ� ûũ ũ�� (��ū)
                chunkOverlapTokens: 100     // ûũ �� �ߺ� ��ū ��
            );
        
        var file = await vectorStoreClient.CreateAndPollVectorStoreFileAsync(
            vectorStore.Id,
            request
        );
        
        Console.WriteLine($"File processed with static chunking strategy: {file.Status}");
        
        // 3. �ڵ� ûŷ �������� ���� ��ġ ���ε�
        var fileIds = new List<string> { "file-def456", "file-ghi789" }; // �̹� ���ε�� ���� ID
        
        var batchRequest = CreateVectorStoreFileBatchRequest.Create(fileIds)
            .WithAutoChunkingStrategy();
        
        var batch = await vectorStoreClient.CreateAndPollVectorStoreFileBatchAsync(
            vectorStore.Id,
            batchRequest
        );
        
        Console.WriteLine($"Batch processed with auto chunking strategy: {batch.Status}");
        Console.WriteLine($"Completed: {batch.FileCounts.Completed}, Failed: {batch.FileCounts.Failed}");
        
        // 4. Vector Store ���� ��å ����
        var updateRequest = UpdateVectorStoreRequest.Create()
            .WithLastActiveExpirationPolicy(days: 30);  // 30�� ���� ��Ȱ�� �� ����
        
        await vectorStoreClient.UpdateVectorStoreAsync(vectorStore.Id, updateRequest);
        
        Console.WriteLine("Vector Store updated with expiration policy");
    }
}
```

## Ȯ�� ������ ������

���̺귯���� `BaseModel` Ŭ������ ���� ���� �Ӽ� ������ �����մϴ�:

```csharp
// �������� �Ӽ� ��������
var tools = assistant.GetTools();

// �������� �Ӽ� �����ϱ�
assistant.SetTools(new List<Tool> { Tool.CreateCodeInterpreterTool() });

// �Ӽ� ���� ���� Ȯ��
if (assistant.HasProperty("tool_resources"))
{
    var toolResources = assistant.GetValue<object>("tool_resources");
    // ...
}
```

## Vector Store ��� �� ����

OpenAI�� Vector Store API�� ���� ������ ���� ����� Ȱ���� �� �ֽ��ϴ�:

- **���� ����� ���� �� ����**: ������ ���� �Ӻ����� ���� ���� ����� ����
- **���� �߰� �� ûŷ**: �پ��� ûŷ ����(�ڵ�/����)���� ������ ȿ�������� ���� ����
- **���� ��ġ ó��**: ���� ������ �� ���� ó���ϴ� ��ġ �۾� ����
- **���� ��å**: ����� ���ҽ� ������ ���� ���� ��å ���� ����
- **Assistants API ����**: File Search ������ ���� ���� �˻� Ȱ��
- **���� �� ���� �ο�**: ���信 ���� ���� ������ ������ �ο� ����

Vector Store ����� ����ϸ� ��Ը� ���� �÷��ǿ��� �ǹ��� �˻��� �����ϰ�, RAG(Retrieval Augmented Generation) ���ø����̼��� ���� ������ �� �ֽ��ϴ�.

## ���̼���

MIT

## �⿩�ϱ�

�̽��� Ǯ ������Ʈ�� ȯ���մϴ�! �⿩�Ͻñ� ���� ������Ʈ�� �ڵ� ��Ÿ�ϰ� �⿩ ���̵������ Ȯ�����ּ���.