using LMSupplyDepot.Tools.OpenAI.Models;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace LMSupplyDepot.Tools.OpenAI.SampleConsoleApp.Examples;

public class InteractiveAssistantQuery
{
    private static string ApiKey;
    private static OpenAIClient _client;
    private static OpenAIAssistantsClient _assistantClient;
    private static VectorStoreClient _vectorStoreClient;

    static async Task Main(string[] args)
    {
        Console.WriteLine("OpenAI 인터랙티브 어시스턴트 질의 시스템");
        Console.WriteLine("======================================");

        // API 키 로드
        LoadApiKey();

        // 클라이언트 초기화
        _client = new OpenAIClient(ApiKey);
        _assistantClient = new OpenAIAssistantsClient(ApiKey);
        _vectorStoreClient = new VectorStoreClient(ApiKey);

        // 인터랙티브 세션 실행
        await RunInteractiveSessionAsync();
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
            Console.WriteLine("API 키를 찾을 수 없습니다. appsettings.json 파일을 확인해주세요.");
            Environment.Exit(1);
        }
    }

    private static async Task RunInteractiveSessionAsync()
    {
        try
        {
            while (true)
            {
                ClearConsole();

                // 1. 어시스턴트 목록에서 선택
                var assistant = await SelectAssistantAsync();
                if (assistant == null) return;

                ClearConsole();
                Console.WriteLine($"선택한 어시스턴트: {assistant.Id} - {assistant.Name ?? "이름 없음"}");

                // 2. 벡터 스토리지 목록에서 선택 (선택 안 할 수도 있음)
                var vectorStoreIds = await SelectVectorStoresAsync();

                ClearConsole();
                Console.WriteLine($"선택한 어시스턴트: {assistant.Id} - {assistant.Name ?? "이름 없음"}");

                if (vectorStoreIds.Count > 0)
                {
                    Console.WriteLine($"선택한 벡터 스토어: {string.Join(", ", vectorStoreIds)}");
                }
                else
                {
                    Console.WriteLine("벡터 스토어 없이 어시스턴트의 기본 지식으로 질의합니다.");
                }

                // 3. 질의 입력
                string query = GetUserQuery();
                if (string.IsNullOrWhiteSpace(query)) continue;

                // 4. 쿼리 실행
                await ExecuteQueryAsync(assistant.Id, vectorStoreIds, query);

                Console.WriteLine("\n다시 시작하려면 아무 키나 누르세요, 종료하려면 'q'를 누르세요...");
                var key = Console.ReadKey();
                if (key.KeyChar == 'q' || key.KeyChar == 'Q')
                {
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"오류 발생: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"내부 오류: {ex.InnerException.Message}");
            }
        }
    }

    private static async Task<Assistant> SelectAssistantAsync()
    {
        Console.WriteLine("\n=== 사용 가능한 어시스턴트 목록 ===");

        // 어시스턴트 목록 가져오기
        var assistantsResponse = await _assistantClient.ListAssistantsAsync(limit: 25);
        var assistants = assistantsResponse.Data;

        if (assistants == null || assistants.Count == 0)
        {
            Console.WriteLine("사용 가능한 어시스턴트가 없습니다. 어시스턴트를 먼저 생성해주세요.");
            return null;
        }

        // 어시스턴트 목록 출력
        for (int i = 0; i < assistants.Count; i++)
        {
            var assistant = assistants[i];
            Console.WriteLine($"{i + 1}. {assistant.Id} - {assistant.Name ?? "이름 없음"} ({assistant.Model})");
        }

        Console.WriteLine("\n어시스턴트 번호를 선택하세요 (종료: q): ");
        var input = Console.ReadLine();

        if (input?.ToLower() == "q")
        {
            return null;
        }

        if (int.TryParse(input, out int selection) && selection >= 1 && selection <= assistants.Count)
        {
            return assistants[selection - 1];
        }

        Console.WriteLine("잘못된 선택입니다. 다시 시도해주세요.");
        Console.WriteLine("계속하려면 아무 키나 누르세요...");
        Console.ReadKey();
        return await SelectAssistantAsync();
    }

    private static async Task<List<string>> SelectVectorStoresAsync()
    {
        List<string> selectedVectorStores = new List<string>();

        Console.WriteLine("\n=== 벡터 스토어 선택 (선택 사항) ===");
        Console.WriteLine("벡터 스토어를 사용하지 않으려면 0을 입력하세요.");

        // 벡터 스토어 목록 가져오기
        var vectorStoresResponse = await _vectorStoreClient.ListVectorStoresAsync(limit: 25);
        var vectorStores = vectorStoresResponse.Data;

        if (vectorStores == null || vectorStores.Count == 0)
        {
            Console.WriteLine("사용 가능한 벡터 스토어가 없습니다. 벡터 스토어 없이 진행합니다.");
            return selectedVectorStores;
        }

        // 벡터 스토어 목록 출력
        for (int i = 0; i < vectorStores.Count; i++)
        {
            var vectorStore = vectorStores[i];
            Console.WriteLine($"{i + 1}. {vectorStore.Id} - {vectorStore.Name} (상태: {vectorStore.Status})");
        }

        Console.WriteLine("\n벡터 스토어 번호를 선택하세요 (여러 개 선택 시 쉼표로 구분, 선택 안함: 0): ");
        var input = Console.ReadLine();

        if (input == "0" || string.IsNullOrWhiteSpace(input))
        {
            return selectedVectorStores;
        }

        var selections = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var selection in selections)
        {
            if (int.TryParse(selection.Trim(), out int index) && index >= 1 && index <= vectorStores.Count)
            {
                selectedVectorStores.Add(vectorStores[index - 1].Id);
            }
        }

        return selectedVectorStores;
    }

    private static string GetUserQuery()
    {
        Console.WriteLine("\n=== 질의 입력 ===");
        Console.WriteLine("질문을 입력하세요 (이전으로: 0): ");
        var query = Console.ReadLine();

        if (query == "0")
        {
            return string.Empty;
        }

        return query;
    }

    private static async Task ExecuteQueryAsync(string assistantId, List<string> vectorStoreIds, string query)
    {
        Console.WriteLine("\n질의를 처리 중입니다...");

        // 스레드 생성
        var thread = await _assistantClient.CreateThreadAsync();
        Console.WriteLine($"스레드 생성됨: {thread.Id}");

        try
        {
            // 벡터 스토어가 있다면 스레드에 연결
            if (vectorStoreIds.Count > 0)
            {
                var toolResources = new Dictionary<string, object>
                {
                    ["file_search"] = new Dictionary<string, object>
                    {
                        ["vector_store_ids"] = vectorStoreIds
                    }
                };

                thread = await _assistantClient.UpdateThreadAsync(
                    thread.Id,
                    UpdateThreadRequest.Create().WithToolResources(toolResources)
                );
                Console.WriteLine("벡터 스토어가 스레드에 연결되었습니다.");
            }

            // 메시지 생성
            var message = await _assistantClient.CreateUserMessageAsync(thread.Id, query);
            Console.WriteLine("사용자 메시지가 추가되었습니다.");

            // 스트리밍으로 응답 받기
            Console.WriteLine("\n=== 응답 ===\n");

            var streamHandler = _assistantClient.PrepareStreamingRun(thread.Id, assistantId);
            var responseBuilder = new StringBuilder();

            // 이벤트 핸들러 설정
            streamHandler.OnData += (streamEvent) => {
                if (streamEvent.Event == StreamEventTypes.ThreadMessageDelta)
                {
                    var messageDelta = streamEvent.GetDataAs<MessageDelta>();
                    var content = messageDelta?.Delta?.Content;

                    if (content != null && content.Count > 0)
                    {
                        foreach (var contentItem in content)
                        {
                            if (contentItem.Type == "text" && contentItem.Text?.Value != null)
                            {
                                string text = contentItem.Text.Value;
                                responseBuilder.Append(text);
                                Console.Write(text);
                            }
                        }
                    }
                }
            };

            // 오류 핸들러
            streamHandler.OnError += (ex) => {
                Console.WriteLine($"\n오류 발생: {ex.Message}");
            };

            // 완료 핸들러
            bool isCompleted = false;
            streamHandler.OnComplete += () => {
                isCompleted = true;
                Console.WriteLine("\n\n응답이 완료되었습니다.");
            };

            // 스트리밍 시작
            await streamHandler.StartStreamingAsync();

            // 완료될 때까지 대기
            while (!isCompleted)
            {
                await Task.Delay(100);
            }

            // 정리
            streamHandler.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n질의 처리 중 오류 발생: {ex.Message}");
        }
    }

    private static void ClearConsole()
    {
        Console.Clear();
        Console.WriteLine("OpenAI 인터랙티브 어시스턴트 질의 시스템");
        Console.WriteLine("======================================\n");
    }
}