using Microsoft.Extensions.Configuration;
using System.Text;

namespace LMSupplyDepot.Tools.OpenAI.SampleConsoleApp.ChatSample;

class Program
{
    private static string ApiKey;
    private static OpenAIService _openAI;
    private static List<(string role, string content)> _conversation = new();

    // Static constructor to load configuration
    static Program()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("secrets.json", optional: true, reloadOnChange: true)
            .Build();
        ApiKey = configuration["OpenAI:ApiKey"];
    }

    static async Task Main(string[] args)
    {
        try
        {
            // Initialize OpenAI service with API key
            _openAI = new OpenAIService(ApiKey);

            bool exitRequested = false;

            while (!exitRequested)
            {
                DisplayMainMenu();
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await SingleChatMode();
                        break;
                    case "2":
                        await ConversationMode();
                        break;
                    case "3":
                        await StreamingChatMode();
                        break;
                    case "0":
                        exitRequested = true;
                        break;
                    default:
                        Console.WriteLine("잘못된 선택입니다. 다시 시도하세요.");
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"오류 발생: {ex.Message}");
        }
    }

    #region 메뉴 및 기본 UI

    private static void DisplayMainMenu()
    {
        Console.Clear();
        Console.WriteLine("=== OpenAI 채팅 데모 ===");
        Console.WriteLine("1. 단일 메시지 모드");
        Console.WriteLine("2. 대화 모드");
        Console.WriteLine("3. 스트리밍 모드");
        Console.WriteLine("0. 종료");
        Console.Write("선택: ");
    }

    private static string GetUserInput(string prompt)
    {
        Console.WriteLine(prompt);
        Console.Write("> ");

        StringBuilder input = new StringBuilder();
        string line;

        // 사용자가 빈 줄을 입력할 때까지 여러 줄을 입력받음
        while (!string.IsNullOrWhiteSpace(line = Console.ReadLine()))
        {
            input.AppendLine(line);
            Console.Write("> ");
        }

        return input.ToString().Trim();
    }

    private static void DisplayResponse(string response)
    {
        Console.WriteLine("\n--- 응답 ---");
        Console.WriteLine(response);
        Console.WriteLine("------------");
    }

    #endregion

    #region 채팅 모드

    private static async Task SingleChatMode()
    {
        Console.Clear();
        Console.WriteLine("=== 단일 메시지 모드 ===");
        Console.WriteLine("메시지를 입력하고 빈 줄을 입력하면 메시지가 전송됩니다.");
        Console.WriteLine("또는 '종료'를 입력하여 메인 메뉴로 돌아갑니다.");

        while (true)
        {
            string message = GetUserInput("\n메시지를 입력하세요:");

            if (message.ToLower() == "종료")
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            try
            {
                Console.WriteLine("응답을 기다리는 중...");

                // 시스템 프롬프트 추가 (선택 사항)
                string systemPrompt = "당신은 도움이 되는 AI 비서입니다. 정확하고 간결하게 대답하세요.";

                // 메시지 전송
                string response = await _openAI.Chat.SendMessageAsync(message, systemPrompt);

                // 응답 표시
                DisplayResponse(response);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류: {ex.Message}");
            }

            Console.WriteLine("\n계속하려면 아무 키나 누르세요...");
            Console.ReadKey();
        }
    }

    private static async Task ConversationMode()
    {
        Console.Clear();
        Console.WriteLine("=== 대화 모드 ===");
        Console.WriteLine("이전 대화 내용을 유지하며 대화를 이어갑니다.");
        Console.WriteLine("메시지를 입력하고 빈 줄을 입력하면 메시지가 전송됩니다.");
        Console.WriteLine("'삭제'를 입력하여 대화 기록을 초기화하거나, '종료'를 입력하여 메인 메뉴로 돌아갑니다.");

        // 시스템 메시지로 대화 시작
        if (_conversation.Count == 0)
        {
            _conversation.Add(("system", "당신은 도움이 되는 AI 비서입니다. 정확하고 간결하게 대답하세요."));
        }

        while (true)
        {
            // 현재 대화 상태 표시
            if (_conversation.Count > 1)
            {
                Console.WriteLine("\n--- 현재 대화 내용 ---");
                for (int i = 1; i < _conversation.Count; i++)
                {
                    var (role, content) = _conversation[i];
                    string displayRole = role == "user" ? "사용자" : "AI";
                    Console.WriteLine($"{displayRole}: {content.Length > 50 ? content.Substring(0, 50) + "..." : content}");
                }
                Console.WriteLine("---------------------");
            }

            string message = GetUserInput("\n메시지를 입력하세요:");

            if (message.ToLower() == "종료")
            {
                break;
            }

            if (message.ToLower() == "삭제")
            {
                _conversation.Clear();
                _conversation.Add(("system", "당신은 도움이 되는 AI 비서입니다. 정확하고 간결하게 대답하세요."));
                Console.WriteLine("대화 기록이 초기화되었습니다.");
                Console.WriteLine("\n계속하려면 아무 키나 누르세요...");
                Console.ReadKey();
                continue;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            try
            {
                // 사용자 메시지 추가
                _conversation.Add(("user", message));

                Console.WriteLine("응답을 기다리는 중...");

                // 대화 전송
                string response = await _openAI.Chat.SendConversationAsync(_conversation);

                // AI 응답 추가
                _conversation.Add(("assistant", response));

                // 응답 표시
                DisplayResponse(response);
            }
            catch (Exception ex)
            {
                // 오류 발생 시 마지막 사용자 메시지 제거
                if (_conversation.Count > 0 && _conversation[_conversation.Count - 1].role == "user")
                {
                    _conversation.RemoveAt(_conversation.Count - 1);
                }

                Console.WriteLine($"오류: {ex.Message}");
            }

            Console.WriteLine("\n계속하려면 아무 키나 누르세요...");
            Console.ReadKey();
        }
    }

    private static async Task StreamingChatMode()
    {
        Console.Clear();
        Console.WriteLine("=== 스트리밍 모드 ===");
        Console.WriteLine("응답이 생성되는 대로 실시간으로 표시됩니다.");
        Console.WriteLine("메시지를 입력하고 빈 줄을 입력하면 메시지가 전송됩니다.");
        Console.WriteLine("'종료'를 입력하여 메인 메뉴로 돌아갑니다.");

        // 스트리밍 대화용 대화 기록
        List<(string role, string content)> streamingConversation = new List<(string, string)>
        {
            ("system", "당신은 도움이 되는 AI 비서입니다. 정확하고 간결하게 대답하세요.")
        };

        while (true)
        {
            // 현재 대화 상태 표시
            if (streamingConversation.Count > 1)
            {
                Console.WriteLine("\n--- 현재 대화 내용 ---");
                for (int i = 1; i < streamingConversation.Count; i++)
                {
                    var (role, content) = streamingConversation[i];
                    string displayRole = role == "user" ? "사용자" : "AI";
                    Console.WriteLine($"{displayRole}: {content.Length > 50 ? content.Substring(0, 50) + "..." : content}");
                }
                Console.WriteLine("---------------------");
            }

            string message = GetUserInput("\n메시지를 입력하세요:");

            if (message.ToLower() == "종료")
            {
                break;
            }

            if (message.ToLower() == "삭제")
            {
                streamingConversation.Clear();
                streamingConversation.Add(("system", "당신은 도움이 되는 AI 비서입니다. 정확하고 간결하게 대답하세요."));
                Console.WriteLine("대화 기록이 초기화되었습니다.");
                Console.WriteLine("\n계속하려면 아무 키나 누르세요...");
                Console.ReadKey();
                continue;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                continue;
            }

            try
            {
                // 사용자 메시지 추가
                streamingConversation.Add(("user", message));

                Console.WriteLine("\n--- 응답 ---");

                // 스트리밍 처리를 위한 콜백
                void HandleStreamingChunk(string chunk)
                {
                    Console.Write(chunk);
                }

                // 스트리밍 대화 전송
                string fullResponse = await _openAI.Chat.StreamConversationAsync(
                    streamingConversation,
                    HandleStreamingChunk);

                // AI 응답 추가
                streamingConversation.Add(("assistant", fullResponse));

                Console.WriteLine("\n------------");
            }
            catch (Exception ex)
            {
                // 오류 발생 시 마지막 사용자 메시지 제거
                if (streamingConversation.Count > 0 && streamingConversation[streamingConversation.Count - 1].role == "user")
                {
                    streamingConversation.RemoveAt(streamingConversation.Count - 1);
                }

                Console.WriteLine($"\n오류: {ex.Message}");
            }

            Console.WriteLine("\n계속하려면 아무 키나 누르세요...");
            Console.ReadKey();
        }
    }

    #endregion
}