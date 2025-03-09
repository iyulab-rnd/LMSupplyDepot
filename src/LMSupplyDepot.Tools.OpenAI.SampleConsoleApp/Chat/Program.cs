using Microsoft.Extensions.Configuration;
using LMSupplyDepot.Tools.OpenAI;
using LMSupplyDepot.Tools.OpenAI.Models;

namespace AssistantChatSample;

class Program
{
    private static string ApiKey;

    static async Task Main(string[] args)
    {
        // API 키 로드
        LoadApiKey();

        // OpenAI 어시스턴트 클라이언트 초기화
        var client = new OpenAIAssistantsClient(ApiKey);

        try
        {
            // 어시스턴트 목록 조회
            var assistants = await ListAssistantsAsync(client);
            if (assistants.Count == 0)
            {
                Console.WriteLine("등록된 어시스턴트가 없습니다.");
                return;
            }

            // 어시스턴트 선택
            var selectedAssistant = SelectAssistant(assistants);
            Console.WriteLine($"선택된 어시스턴트: {selectedAssistant.Name} (ID: {selectedAssistant.Id})");
            Console.WriteLine("---------------------------------------------------");

            // 어시스턴트와 대화 시작
            await StartConversationAsync(client, selectedAssistant);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"오류 발생: {ex.Message}");
        }
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
            Console.WriteLine("API 키가 설정되지 않았습니다. appsettings.json 파일을 확인하세요.");
            Environment.Exit(1);
        }
    }

    private static async Task<List<Assistant>> ListAssistantsAsync(OpenAIAssistantsClient client)
    {
        Console.WriteLine("등록된 어시스턴트 목록을 불러오는 중...");

        var response = await client.ListAssistantsAsync();
        var assistants = new List<Assistant>();

        foreach (var assistant in response.Data)
        {
            assistants.Add(assistant);
        }

        return assistants;
    }

    private static Assistant SelectAssistant(List<Assistant> assistants)
    {
        Console.WriteLine("\n사용 가능한 어시스턴트 목록:");
        Console.WriteLine("---------------------------------------------------");

        for (int i = 0; i < assistants.Count; i++)
        {
            var assistant = assistants[i];
            Console.WriteLine($"{i + 1}. {assistant.Name} - {assistant.Description}");
        }

        Console.WriteLine("---------------------------------------------------");
        Console.Write("사용할 어시스턴트 번호를 선택하세요: ");

        int selectedIndex;
        while (!int.TryParse(Console.ReadLine(), out selectedIndex) || selectedIndex < 1 || selectedIndex > assistants.Count)
        {
            Console.Write("올바른 번호를 입력하세요: ");
        }

        return assistants[selectedIndex - 1];
    }

    private static async Task StartConversationAsync(OpenAIAssistantsClient client, Assistant assistant)
    {
        Console.WriteLine($"\n{assistant.Name}와의 대화를 시작합니다. 종료하려면 'exit' 또는 'quit'를 입력하세요.");
        Console.WriteLine("---------------------------------------------------");

        // 새 스레드 생성
        var thread = await client.CreateThreadAsync();
        Console.WriteLine($"새 대화 스레드가 생성되었습니다. (ID: {thread.Id})");

        while (true)
        {
            // 사용자 메시지 입력 받기
            Console.Write("\n나: ");
            var userMessage = Console.ReadLine();

            if (string.IsNullOrWhiteSpace(userMessage))
                continue;

            if (userMessage.ToLower() == "exit" || userMessage.ToLower() == "quit")
                break;

            // 사용자 메시지 전송
            await client.CreateUserMessageAsync(thread.Id, userMessage);

            // 어시스턴트 실행
            Console.WriteLine("\n메시지 처리 중...");
            var run = await client.CreateSimpleRunAsync(thread.Id, assistant.Id);

            try
            {
                // 응답 대기
                run = await client.WaitForRunCompletionAsync(thread.Id, run.Id);

                if (run.Status == RunStatus.Completed)
                {
                    // 어시스턴트 응답 조회
                    var messages = await client.ListMessagesAsync(thread.Id, order: "desc", limit: 1);
                    var assistantMessage = messages.Data[0];

                    if (assistantMessage.Role == "assistant")
                    {
                        var messageText = GetMessageText(assistantMessage);
                        Console.WriteLine($"\n{assistant.Name}: {messageText}");
                    }
                }
                else
                {
                    Console.WriteLine($"\n오류: 실행 상태 - {run.Status}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n오류 발생: {ex.Message}");
            }
        }

        Console.WriteLine("\n대화를 종료합니다.");
    }

    private static string GetMessageText(Message message)
    {
        if (message?.Content == null || message.Content.Count == 0)
        {
            return string.Empty;
        }

        var textContentList = new List<string>();
        foreach (var content in message.Content)
        {
            if (content.Type == "text")
            {
                var textContent = content.GetTextContent();
                if (textContent != null && !string.IsNullOrEmpty(textContent.Value))
                {
                    textContentList.Add(textContent.Value);
                }
            }
        }

        return string.Join("\n", textContentList);
    }
}