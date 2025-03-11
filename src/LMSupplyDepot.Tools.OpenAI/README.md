# LMSupplyDepot.Tools.OpenAI

C# 라이브러리로 OpenAI의 Assistants API, Chat API 및 Vector Store API와 쉽게 통합할 수 있습니다.

## 특징

- OpenAI Assistants API 전체 기능 지원
- Chat Completions API 지원
- 임베딩 API 지원
- Vector Store API 지원
- 모델 탐색 및 모델 추천 기능
- 유연한 모델 설계로 API 변경에 대응 가능
- 편리한 헬퍼 메서드로 간단한 통합 가능
- 동적 속성 접근 방식으로 확장성 제공

## 설치

NuGet 패키지 관리자를 통해 설치할 수 있습니다:

```bash
dotnet add package LMSupplyDepot.Tools.OpenAI
```

## 빠른 시작

### Chat Completions API 사용하기

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // API 키로 클라이언트 초기화
        var chatClient = new OpenAIClient("your-api-key-here");

        // 간단한 요청으로 응답 가져오기
        var completion = await chatClient.CreateSimpleChatCompletionAsync(
            "gpt-4o",
            "3x + 11 = 14라는 방정식을 풀어줄 수 있을까요?"
        );

        // 응답 출력
        Debug.WriteLine(chatClient.GetCompletionText(completion));
    }
}
```

### 함수 호출(Function Calling) 사용하기

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using System;
using System.Text.Json;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var chatClient = new OpenAIClient("your-api-key-here");

        // 함수 스키마 정의
        var weatherFunctionParams = new
        {
            type = "object",
            properties = new
            {
                location = new
                {
                    type = "string",
                    description = "도시 이름(예: 서울, 부산)"
                },
                unit = new
                {
                    type = "string",
                    enum = new[] { "celsius", "fahrenheit" },
                    description = "온도 단위"
                }
            },
            required = new[] { "location" }
        };

        // 함수 도구를 포함한 요청 생성
        var request = CreateChatCompletionRequest.Create(
            "gpt-4o",
            new List<ChatMessage> { ChatMessage.FromUser("서울의 날씨가 어떤지 알려주세요.") }
        ).WithFunctionTool(
            "get_weather",
            "특정 위치의 현재 날씨 정보를 가져옵니다.",
            weatherFunctionParams
        );

        // 요청 보내기
        var completion = await chatClient.CreateChatCompletionAsync(request);

        // 함수 호출 확인
        if (chatClient.HasToolCalls(completion))
        {
            var toolCalls = chatClient.GetToolCalls(completion);
            foreach (var toolCall in toolCalls)
            {
                if (toolCall.Type == "function" && toolCall.GetFunctionName() == "get_weather")
                {
                    // 함수 인자 파싱
                    var args = toolCall.GetFunctionArguments();
                    Debug.WriteLine($"함수 호출: {toolCall.GetFunctionName()}, 인자: {args}");

                    // 여기서 실제 날씨 정보를 가져오는 로직 구현
                    // ...

                    // 함수 실행 결과로 대화 계속하기
                    var toolOutputs = new List<ToolOutput>
                    {
                        ToolOutput.Create(
                            toolCall.Id,
                            JsonSerializer.Serialize(new
                            {
                                location = "서울",
                                temperature = 22.5,
                                unit = "celsius",
                                condition = "맑음"
                            })
                        )
                    };

                    // 함수 호출 결과로 대화 계속
                    var followUpCompletion = await chatClient.ContinueWithToolOutputsAsync(
                        "gpt-4o",
                        completion.Choices[0].Message,
                        toolOutputs
                    );

                    Debug.WriteLine(chatClient.GetCompletionText(followUpCompletion));
                }
            }
        }
    }
}
```

### 대화 기록 만들기

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

        // 대화 기록 만들기
        var messages = ChatClientHelpers.BuildConversationHistory(
            "안녕하세요!", // 유저 메시지
            "안녕하세요! 무엇을 도와드릴까요?", // 어시스턴트 메시지
            "GPT-4와 GPT-4o의 차이점이 뭔가요?" // 유저 메시지
        );

        // 개발자(시스템) 메시지 추가
        messages.Insert(0, ChatMessage.FromDeveloper("당신은 OpenAI 모델에 대해 설명하는 전문가입니다."));

        // 채팅 완성 요청 생성
        var request = CreateChatCompletionRequest.Create("gpt-4o", messages)
            .WithTemperature(0.7)
            .WithMaxTokens(500);

        // 요청 보내기
        var completion = await chatClient.CreateChatCompletionAsync(request);

        // 응답 출력
        Debug.WriteLine(chatClient.GetCompletionText(completion));
    }
}
```

### 이미지 입력이 있는 멀티모달 메시지

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

        // 멀티모달 메시지 생성 (텍스트와 이미지 동시에 포함)
        var multimodalMessage = ChatClientHelpers.CreateMultimodalMessage(
            "이 이미지에 무엇이 있나요?",
            "https://example.com/image.jpg"
        );

        // 채팅 완성 요청 생성
        var request = CreateChatCompletionRequest.Create(
            "gpt-4o",
            new List<ChatMessage> { multimodalMessage }
        );

        // 요청 보내기
        var completion = await chatClient.CreateChatCompletionAsync(request);

        // 응답 출력
        Debug.WriteLine(chatClient.GetCompletionText(completion));
    }
}
```

### JSON 형식 응답 받기

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

        // JSON 스키마 정의
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

        // 채팅 완성 요청 생성
        var request = CreateChatCompletionRequest.Create(
            "gpt-4o",
            new List<ChatMessage> { ChatMessage.FromUser("이름은 홍길동, 나이는 30살, 이메일은 hong@example.com인 사람의 정보를 JSON으로 정리해주세요.") }
        ).WithResponseFormat(ChatClientHelpers.CreateJsonResponseFormat(schema));

        // 요청 보내기
        var completion = await chatClient.CreateChatCompletionAsync(request);

        // 응답 출력
        Debug.WriteLine(chatClient.GetCompletionText(completion));
    }
}
```

### 임베딩 생성하기

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

        // 텍스트 임베딩 생성
        var embeddingsResponse = await chatClient.CreateEmbeddingAsync(
            "text-embedding-3-small",
            "OpenAI의 임베딩 API는 텍스트를 수치 벡터로 변환합니다."
        );

        // 임베딩 벡터 가져오기
        var embeddingVector = chatClient.GetEmbeddingVector(embeddingsResponse);
        
        Debug.WriteLine($"임베딩 차원 수: {embeddingVector.Count}");
        
        // 차원 수 지정하여 임베딩 생성 (짧은 임베딩)
        var shorterEmbeddingsResponse = await chatClient.CreateEmbeddingAsync(
            "text-embedding-3-small",
            "OpenAI의 임베딩 API는 텍스트를 수치 벡터로 변환합니다.",
            dimensions: 256
        );
        
        var shorterVector = chatClient.GetEmbeddingVector(shorterEmbeddingsResponse);
        Debug.WriteLine($"축소된 임베딩 차원 수: {shorterVector.Count}");
        
        // 임베딩으로 의미적 유사도 계산
        var secondEmbeddingsResponse = await chatClient.CreateEmbeddingAsync(
            "text-embedding-3-small",
            "텍스트를 수치적 표현으로 변환하는 OpenAI의 기술입니다."
        );
        
        var secondVector = chatClient.GetEmbeddingVector(secondEmbeddingsResponse);
        
        double similarity = ChatClientHelpers.CosineSimilarity(embeddingVector, secondVector);
        Debug.WriteLine($"텍스트 유사도: {similarity}");
    }
}
```

### 모델 정보 탐색하기

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

        // 모든 모델 목록 가져오기
        var models = await modelExplorer.ListAllModelsAsync();
        Debug.WriteLine($"사용 가능한 모델 수: {models.Data.Count}");

        // GPT 모델만 가져오기
        var gptModels = await modelExplorer.GetGptModelsAsync();
        Debug.WriteLine("\nGPT 모델:");
        foreach (var model in gptModels)
        {
            Debug.WriteLine($"- {model.Id}: {model.Description}");
        }

        // 임베딩 모델만 가져오기
        var embeddingModels = await modelExplorer.GetEmbeddingsModelsAsync();
        Debug.WriteLine("\n임베딩 모델:");
        foreach (var model in embeddingModels)
        {
            Debug.WriteLine($"- {model.Id}: {model.Description}");
        }

        // 작업에 맞는 모델 추천 받기
        var suggestedModel = await modelExplorer.SuggestModelForTaskAsync(
            "한국어와 영어가 혼합된 고객 리뷰 데이터에서 감정 분석을 수행해야 합니다."
        );

        Debug.WriteLine($"\n추천 모델: {suggestedModel.Id}");
        Debug.WriteLine($"모델 설명: {suggestedModel.Description}");
        Debug.WriteLine($"컨텍스트 윈도우: {suggestedModel.ContextWindow} 토큰");
    }
}
```

### Assistants API 사용하기

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
        // API 키로 클라이언트 초기화
        var assistantsClient = new OpenAIAssistantsClient("your-api-key-here");

        // Assistant 생성
        var assistant = await assistantsClient.CreateAssistantAsync(
            CreateAssistantRequest.Create("gpt-4o")
                .WithName("Math Tutor")
                .WithInstructions("수학 문제를 해결하는 데 도움을 주는 튜터입니다.")
                .WithTools(new List<Tool> { Tool.CreateCodeInterpreterTool() })
        );

        Debug.WriteLine($"Assistant created with ID: {assistant.Id}");

        // Thread 생성 및 메시지 추가
        var thread = await assistantsClient.CreateThreadAsync();
        
        // 사용자 메시지 추가
        await assistantsClient.CreateUserMessageAsync(thread.Id, "3x + 11 = 14라는 방정식을 풀어줄 수 있을까요?");
        
        // Run 생성 및 실행
        var run = await assistantsClient.CreateSimpleRunAsync(thread.Id, assistant.Id);
        
        // Run이 완료될 때까지 대기
        run = await assistantsClient.WaitForRunCompletionAsync(thread.Id, run.Id);
        
        // 응답 메시지 가져오기
        var messages = await assistantsClient.ListMessagesAsync(thread.Id, order: "desc", limit: 1);
        var assistantMessage = messages.Data[0];
        
        // 응답 출력
        Debug.WriteLine("Assistant's response:");
        Debug.WriteLine(AssistantHelpers.GetMessageText(assistantMessage));
    }
}
```

### 간단한 대화 생성하기

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
        var assistantsClient = new OpenAIAssistantsClient("your-api-key-here");

        // 간단한 대화 생성 (Assistant ID가 필요)
        var assistantMessage = await assistantsClient.GetSimpleResponseAsync(
            "asst_123456789", // 실제 Assistant ID로 대체
            "행렬 곱셈의 계산 방법을 설명해주세요."
        );

        // 응답 출력
        Debug.WriteLine(AssistantHelpers.GetMessageText(assistantMessage));

        // 또는 풍부한 대화 기록 가져오기
        var messages = await AssistantHelpers.RunConversationAsync(
            assistantsClient,
            "asst_123456789", // 실제 Assistant ID로 대체
            new List<string> {
                "행렬이 무엇인가요?",
                "2x2 행렬의 예를 들어주세요.",
                "이 행렬의 행렬식을 계산하는 방법은?"
            }
        );

        // 전체 대화 출력
        foreach (var msg in messages)
        {
            Debug.WriteLine($"{msg.Role}: {AssistantHelpers.GetMessageText(msg)}");
            Debug.WriteLine("---");
        }
    }
}
```

### Vector Store API 사용하기

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        // Vector Store 클라이언트 초기화
        var vectorStoreClient = new VectorStoreClient("your-api-key-here");
        
        // Vector Store 생성
        var vectorStore = await vectorStoreClient.CreateVectorStoreAsync("My Knowledge Base");
        Debug.WriteLine($"Vector Store created with ID: {vectorStore.Id}");
        
        // 파일 업로드 및 Vector Store에 추가 (OpenAIAssistantsClient 필요)
        var assistantsClient = new OpenAIAssistantsClient("your-api-key-here");
        
        // 단일 파일 업로드 및 추가
        var vectorStoreFile = await vectorStoreClient.UploadAndAddFileAsync(
            vectorStore.Id,
            "path/to/document.pdf",
            assistantsClient
        );
        
        Debug.WriteLine($"File added to Vector Store with ID: {vectorStoreFile.Id}");
        
        // 여러 파일 업로드 및 새 Vector Store 생성
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
        
        Debug.WriteLine($"New Vector Store with multiple files created: {newVectorStore.Id}");
        Debug.WriteLine($"Total files: {newVectorStore.GetFileCounts().Total}");
        
        // Vector Store 목록 가져오기
        var vectorStores = await vectorStoreClient.ListVectorStoresAsync(limit: 10);
        
        Debug.WriteLine("\nAvailable Vector Stores:");
        foreach (var store in vectorStores.Data)
        {
            Debug.WriteLine($"- {store.Name} (ID: {store.Id}, Status: {store.Status})");
        }
        
        // 일괄 파일 업로드 (File Batch)
        var fileIds = new List<string> { "file-abc123", "file-def456" }; // 이미 업로드된 파일 ID
        var batch = await vectorStoreClient.CreateVectorStoreFileBatchAsync(vectorStore.Id, fileIds);
        
        // 배치 처리가 완료될 때까지 대기
        batch = await vectorStoreClient.PollVectorStoreFileBatchUntilCompletedAsync(
            vectorStore.Id, 
            batch.Id
        );
        
        Debug.WriteLine($"Batch processing completed: {batch.FileCounts.Completed} of {batch.FileCounts.Total} files processed");
        
        // Vector Store를 사용하는 Assistant 생성
        assistant = await assistantsClient.CreateAssistantWithFileSearchAsync(
            "gpt-4o", 
            "문서 검색에 특화된 어시스턴트입니다.", 
            new List<string> { vectorStore.Id },
            "Document Expert"
        );
        
        Debug.WriteLine($"Created an assistant with Vector Store: {assistant.Id}");
    }
}
```

### Vector Store와 함께 File Search 기능 사용하기

```csharp
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;
using System;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        var assistantsClient = new OpenAIAssistantsClient("your-api-key-here");
        var vectorStoreClient = new VectorStoreClient("your-api-key-here");
        
        // 1. Vector Store 생성 및 파일 업로드
        var vectorStore = await vectorStoreClient.CreateVectorStoreAsync("Research Papers");
        
        // 파일 업로드 (이미 업로드된 파일 ID 사용)
        await vectorStoreClient.CreateVectorStoreFileAsync(
            vectorStore.Id, 
            "file-abc123" // 이미 업로드된 파일 ID
        );
        
        // 2. File Search 도구를 갖춘 Assistant 생성
        var assistant = await assistantsClient.CreateAssistantAsync(
            CreateAssistantRequest.Create("gpt-4o")
                .WithName("Research Assistant")
                .WithInstructions("연구 논문에서 정보를 찾고 요약해주는 어시스턴트입니다.")
                .WithTools(new List<Tool> { Tool.CreateFileSearchTool() })
        );
        
        // 3. Vector Store 리소스 추가
        await assistantsClient.UpdateAssistantWithVectorStoresAsync(
            assistant.Id,
            new List<string> { vectorStore.Id }
        );
        
        // 4. Thread 생성 및 질문하기
        var thread = await assistantsClient.CreateThreadWithVectorStoresAsync(
            new List<string> { vectorStore.Id }
        );
        
        // 5. 질문 추가
        await assistantsClient.CreateUserMessageAsync(
            thread.Id,
            "논문에서 딥러닝 모델의 성능 평가 지표에 대한 정보를 찾아주세요."
        );
        
        // 6. 실행
        var run = await assistantsClient.CreateSimpleRunAsync(thread.Id, assistant.Id);
        run = await assistantsClient.WaitForRunCompletionAsync(thread.Id, run.Id);
        
        // 7. 응답 확인 (파일 인용 포함)
        var messages = await assistantsClient.ListMessagesAsync(thread.Id, order: "desc", limit: 1);
        var responseWithCitations = await AssistantHelpers.GetMessageWithCitationsAsync(
            messages.Data[0],
            assistantsClient
        );
        
        Debug.WriteLine("Response with citations:");
        Debug.WriteLine(responseWithCitations);
    }
}
```

### Chunking 전략을 사용한 Vector Store 파일 처리

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
        
        // 1. Vector Store 생성
        var vectorStore = await vectorStoreClient.CreateVectorStoreAsync("Technical Documentation");
        
        // 2. 정적 청킹 전략으로 파일 업로드
        var fileId = "file-abc123"; // 이미 업로드된 파일 ID
        
        var request = CreateVectorStoreFileRequest.Create(fileId)
            .WithStaticChunkingStrategy(
                maxChunkSizeTokens: 1000,  // 최대 청크 크기 (토큰)
                chunkOverlapTokens: 100     // 청크 간 중복 토큰 수
            );
        
        var file = await vectorStoreClient.CreateAndPollVectorStoreFileAsync(
            vectorStore.Id,
            request
        );
        
        Debug.WriteLine($"File processed with static chunking strategy: {file.Status}");
        
        // 3. 자동 청킹 전략으로 파일 배치 업로드
        var fileIds = new List<string> { "file-def456", "file-ghi789" }; // 이미 업로드된 파일 ID
        
        var batchRequest = CreateVectorStoreFileBatchRequest.Create(fileIds)
            .WithAutoChunkingStrategy();
        
        var batch = await vectorStoreClient.CreateAndPollVectorStoreFileBatchAsync(
            vectorStore.Id,
            batchRequest
        );
        
        Debug.WriteLine($"Batch processed with auto chunking strategy: {batch.Status}");
        Debug.WriteLine($"Completed: {batch.FileCounts.Completed}, Failed: {batch.FileCounts.Failed}");
        
        // 4. Vector Store 만료 정책 설정
        var updateRequest = UpdateVectorStoreRequest.Create()
            .WithLastActiveExpirationPolicy(days: 30);  // 30일 동안 비활성 시 만료
        
        await vectorStoreClient.UpdateVectorStoreAsync(vectorStore.Id, updateRequest);
        
        Debug.WriteLine("Vector Store updated with expiration policy");
    }
}
```

## 확장 가능한 디자인

라이브러리는 `BaseModel` 클래스를 통해 동적 속성 접근을 제공합니다:

```csharp
// 동적으로 속성 가져오기
var tools = assistant.GetTools();

// 동적으로 속성 설정하기
assistant.SetTools(new List<Tool> { Tool.CreateCodeInterpreterTool() });

// 속성 존재 여부 확인
if (assistant.HasProperty("tool_resources"))
{
    var toolResources = assistant.GetValue<object>("tool_resources");
    // ...
}
```

## Vector Store 기능 상세 설명

OpenAI의 Vector Store API를 통해 다음과 같은 기능을 활용할 수 있습니다:

- **벡터 저장소 생성 및 관리**: 문서와 관련 임베딩을 위한 벡터 저장소 관리
- **파일 추가 및 청킹**: 다양한 청킹 전략(자동/정적)으로 문서를 효율적으로 분할 저장
- **파일 배치 처리**: 여러 파일을 한 번에 처리하는 배치 작업 지원
- **만료 정책**: 저장소 리소스 관리를 위한 만료 정책 설정 가능
- **Assistants API 통합**: File Search 도구를 통해 벡터 검색 활용
- **파일 및 문서 인용**: 응답에 원본 문서 정보를 포함한 인용 지원

Vector Store 기능을 사용하면 대규모 문서 컬렉션에서 의미적 검색을 수행하고, RAG(Retrieval Augmented Generation) 애플리케이션을 쉽게 구축할 수 있습니다.

## 라이선스

MIT

## 기여하기

이슈와 풀 리퀘스트는 환영합니다! 기여하시기 전에 프로젝트의 코드 스타일과 기여 가이드라인을 확인해주세요.