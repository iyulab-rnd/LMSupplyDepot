//using Microsoft.Extensions.Configuration;
//using LMSupplyDepot.Tools.OpenAI.Models;
//using LMSupplyDepot.Tools.OpenAI.Utilities;
//using System.Text.Json;

//namespace LMSupplyDepot.Tools.OpenAI.SampleConsoleApp;

//class Program
//{
//    private static readonly string ApiKey;
//    private static readonly string LocalFolder = @"d:/test";
//    private static readonly string AssistantName = "FileSearchAssistant";

//    // 클라이언트 인스턴스
//    private static OpenAIAssistantsClient _assistantsClient;
//    private static VectorStoreClient _vectorStoreClient;

//    // 파일 추적용 정보
//    private static Dictionary<string, FileInfo> _deployedFiles = new Dictionary<string, FileInfo>();
//    private static Dictionary<string, string> _fileIdMapping = new Dictionary<string, string>(); // 파일 경로 -> 파일 ID
//    private static string _vectorStoreId;
//    private static string _assistantId;
//    private static string _threadId;

//    // 정적 생성자에서 구성 로드
//    static Program()
//    {
//        var configuration = new ConfigurationBuilder()
//            .SetBasePath(Directory.GetCurrentDirectory())
//            .AddJsonFile("secrets.json", optional: true, reloadOnChange: true)
//            .Build();
//        ApiKey = configuration["OpenAI:ApiKey"];
//    }

//    static async Task Main(string[] args)
//    {
//        Console.WriteLine("OpenAI Assistant v2 샘플 애플리케이션 시작");

//        if (string.IsNullOrEmpty(ApiKey))
//        {
//            Console.WriteLine("오류: ApiKey가 설정되지 않았습니다. secrets.json 파일을 확인하세요.");
//            Console.WriteLine("다음 형식으로 secrets.json 파일을 생성하세요:");
//            Console.WriteLine("{");
//            Console.WriteLine("  \"OpenAI\": {");
//            Console.WriteLine("    \"ApiKey\": \"your-api-key\"");
//            Console.WriteLine("  }");
//            Console.WriteLine("}");
//            return;
//        }

//        try
//        {
//            // 클라이언트 초기화
//            _assistantsClient = new OpenAIAssistantsClient(ApiKey);
//            _vectorStoreClient = new VectorStoreClient(ApiKey);

//            // API 연결 테스트
//            Console.WriteLine("API 연결 테스트 중...");
//            var models = await _assistantsClient.ListAssistantsAsync(limit: 1);
//            Console.WriteLine("API 연결 성공");

//            // 초기 시작
//            await InitializeApplicationAsync();

//            // 대화 제거하고 명령 대기 루프 실행
//            await RunCommandLoopAsync();
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"오류 발생: {ex.Message}");
//            if (ex.InnerException != null)
//            {
//                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
//            }
//            Console.WriteLine(ex.StackTrace);
//        }

//        Console.WriteLine("엔터 키를 누르면 종료합니다...");
//        Console.ReadLine();
//    }

//    // 애플리케이션 초기화
//    private static async Task InitializeApplicationAsync()
//    {
//        // 1. 로컬 파일 업로드 및 추적 정보 초기화
//        List<string> fileIds = await UploadLocalFiles();

//        // 2. Vector Store 생성 및 파일 추가
//        _vectorStoreId = await CreateVectorStore(fileIds);

//        // 3. 어시스턴트 생성 또는 가져오기
//        _assistantId = await GetOrCreateAssistant(_vectorStoreId);

//        // 4. 테스트 스레드 생성
//        _threadId = await CreateTestThread();

//        Console.WriteLine("\n초기화가 완료되었습니다.");
//        Console.WriteLine($"어시스턴트 ID: {_assistantId}");
//        Console.WriteLine($"벡터 스토어 ID: {_vectorStoreId}");
//        Console.WriteLine($"스레드 ID: {_threadId}");
//        Console.WriteLine("\n명령어:");
//        Console.WriteLine("r - 폴더 새로고침 및 파일 동기화");
//        Console.WriteLine("q - 애플리케이션 종료");
//    }

//    // 명령 대기 루프
//    private static async Task RunCommandLoopAsync()
//    {
//        bool running = true;
//        while (running)
//        {
//            Console.WriteLine("\n명령을 입력하세요 (r: 리프레시, q: 종료):");
//            var key = Console.ReadKey(true);

//            switch (key.KeyChar)
//            {
//                case 'r':
//                    Console.WriteLine("폴더 새로고침 및 파일 동기화 중...");
//                    await RefreshFilesAsync();
//                    break;
//                case 'q':
//                    running = false;
//                    break;
//                default:
//                    Console.WriteLine("알 수 없는 명령입니다. r 또는 q를 입력하세요.");
//                    break;
//            }
//        }
//    }

//    // 파일 새로고침 및 동기화
//    private static async Task RefreshFilesAsync()
//    {
//        try
//        {
//            // 1. 로컬 폴더 파일 정보 가져오기
//            var currentFiles = GetLocalFiles();
//            var currentFileInfos = currentFiles.ToDictionary(f => f.FullName, f => f);

//            // 2. 추가/변경된 파일 식별 및 업로드
//            var filesToUpdate = new List<string>();
//            var updatedFileIds = new Dictionary<string, string>();

//            foreach (var file in currentFiles)
//            {
//                string filePath = file.FullName;
//                bool isNewOrModified = false;

//                // 기존에 없던 새 파일인지 확인
//                if (!_deployedFiles.ContainsKey(filePath))
//                {
//                    Console.WriteLine($"새 파일 감지: {Path.GetFileName(filePath)}");
//                    isNewOrModified = true;
//                }
//                // 기존 파일이 수정되었는지 확인 (크기 또는 수정 시간으로)
//                else if (_deployedFiles[filePath].Length != file.Length ||
//                         _deployedFiles[filePath].LastWriteTime != file.LastWriteTime)
//                {
//                    Console.WriteLine($"수정된 파일 감지: {Path.GetFileName(filePath)}");
//                    isNewOrModified = true;
//                }

//                if (isNewOrModified)
//                {
//                    filesToUpdate.Add(filePath);
//                }
//            }

//            // 3. 파일 업로드 처리
//            foreach (var filePath in filesToUpdate)
//            {
//                string fileName = Path.GetFileName(filePath);
//                Console.WriteLine($"업로드 중: {fileName}");

//                try
//                {
//                    // 기존 파일이 있으면 삭제
//                    if (_fileIdMapping.TryGetValue(filePath, out string oldFileId))
//                    {
//                        Console.WriteLine($"기존 파일 ID({oldFileId}) 삭제 중...");
//                        try
//                        {
//                            await _assistantsClient.DeleteFileAsync(oldFileId);
//                            if (_vectorStoreId != null)
//                            {
//                                try
//                                {
//                                    await _vectorStoreClient.DeleteVectorStoreFileAsync(_vectorStoreId, oldFileId);
//                                }
//                                catch
//                                {
//                                    // 벡터 스토어에 없는 파일일 수 있음 - 무시
//                                }
//                            }
//                        }
//                        catch (Exception ex)
//                        {
//                            Console.WriteLine($"파일 삭제 중 오류 발생: {ex.Message} - 계속 진행합니다.");
//                        }
//                    }

//                    // 새 파일 업로드
//                    var fileResponse = await _assistantsClient.UploadFileAsync(filePath, "assistants");
//                    string fileId = JsonSerializer.Deserialize<JsonElement>(fileResponse.ToString()).GetProperty("id").GetString();

//                    if (!string.IsNullOrEmpty(fileId))
//                    {
//                        updatedFileIds[filePath] = fileId;
//                        Console.WriteLine($"파일 업로드 완료. ID: {fileId}");

//                        // Vector Store에 파일 추가
//                        if (_vectorStoreId != null)
//                        {
//                            try
//                            {
//                                await _vectorStoreClient.CreateAndPollVectorStoreFileAsync(
//                                    _vectorStoreId,
//                                    CreateVectorStoreFileRequest.Create(fileId)
//                                );
//                                Console.WriteLine($"파일이 Vector Store에 추가되었습니다.");
//                            }
//                            catch (Exception ex)
//                            {
//                                Console.WriteLine($"Vector Store에 파일 추가 중 오류 발생: {ex.Message}");
//                            }
//                        }
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"'{fileName}' 파일 업로드 중 오류 발생: {ex.Message}");
//                }
//            }

//            // 4. 삭제된 파일 처리
//            var deletedFiles = _deployedFiles.Keys.Where(path => !currentFileInfos.ContainsKey(path)).ToList();
//            foreach (var filePath in deletedFiles)
//            {
//                string fileName = Path.GetFileName(filePath);
//                Console.WriteLine($"삭제된 파일 감지: {fileName}");

//                if (_fileIdMapping.TryGetValue(filePath, out string fileId))
//                {
//                    try
//                    {
//                        Console.WriteLine($"OpenAI에서 파일 삭제 중 (ID: {fileId})...");
//                        await _assistantsClient.DeleteFileAsync(fileId);

//                        // Vector Store에서도 삭제
//                        if (_vectorStoreId != null)
//                        {
//                            try
//                            {
//                                await _vectorStoreClient.DeleteVectorStoreFileAsync(_vectorStoreId, fileId);
//                                Console.WriteLine($"파일이 Vector Store에서 삭제되었습니다.");
//                            }
//                            catch
//                            {
//                                // 벡터 스토어에 없는 파일일 수 있음 - 무시
//                            }
//                        }

//                        // 매핑에서 제거
//                        _fileIdMapping.Remove(filePath);
//                    }
//                    catch (Exception ex)
//                    {
//                        Console.WriteLine($"'{fileName}' 파일 삭제 중 오류 발생: {ex.Message}");
//                    }
//                }
//            }

//            // 5. 파일 정보 업데이트
//            foreach (var filePath in filesToUpdate)
//            {
//                if (updatedFileIds.TryGetValue(filePath, out string fileId))
//                {
//                    _fileIdMapping[filePath] = fileId;
//                    _deployedFiles[filePath] = new FileInfo(filePath);
//                }
//            }

//            // 삭제된 파일 정보 제거
//            foreach (var filePath in deletedFiles)
//            {
//                _deployedFiles.Remove(filePath);
//            }

//            // 6. 어시스턴트가 업데이트된 Vector Store를 사용하도록 갱신
//            if (_assistantId != null && _vectorStoreId != null)
//            {
//                Console.WriteLine("어시스턴트 구성 업데이트 중...");
//                var updateRequest = UpdateAssistantRequest.Create()
//                    .WithTools(new List<Tool> { Tool.CreateFileSearchTool() });

//                var toolResources = AssistantHelpers.CreateFileSearchToolResources(new List<string> { _vectorStoreId });
//                updateRequest.WithToolResources(toolResources);

//                await _assistantsClient.UpdateAssistantAsync(_assistantId, updateRequest);
//                Console.WriteLine("어시스턴트가 성공적으로 업데이트되었습니다.");
//            }

//            Console.WriteLine("파일 동기화가 완료되었습니다.");
//            Console.WriteLine($"총 {_deployedFiles.Count}개의 파일이 추적 중입니다.");
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"파일 동기화 중 오류 발생: {ex.Message}");
//        }
//    }

//    // 로컬 폴더의 파일 목록 가져오기
//    private static List<FileInfo> GetLocalFiles()
//    {
//        try
//        {
//            if (!Directory.Exists(LocalFolder))
//            {
//                Console.WriteLine($"경고: 지정된 폴더 '{LocalFolder}'가 존재하지 않습니다. 폴더를 생성합니다.");
//                Directory.CreateDirectory(LocalFolder);
//            }

//            var allowedExtensions = new[] { ".txt", ".pdf", ".doc", ".docx", ".csv", ".json", ".md", ".html" };

//            // 지원되는 확장자 파일 검색
//            var files = Directory.GetFiles(LocalFolder)
//                .Where(file => allowedExtensions.Contains(Path.GetExtension(file).ToLower()))
//                .Select(file => new FileInfo(file))
//                .ToList();

//            Console.WriteLine($"{files.Count}개의 파일을 찾았습니다.");

//            // 파일이 없을 경우 샘플 파일 생성
//            if (files.Count == 0)
//            {
//                Console.WriteLine("폴더에 파일이 없습니다. 샘플 텍스트 파일을 생성합니다.");
//                string sampleFilePath = Path.Combine(LocalFolder, "sample.txt");
//                File.WriteAllText(sampleFilePath, "이것은 OpenAI 어시스턴트 테스트를 위한 샘플 파일입니다. 이 파일에는 테스트 데이터가 포함되어 있습니다.");
//                files.Add(new FileInfo(sampleFilePath));
//            }

//            return files;
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"로컬 파일 목록 가져오기 중 오류 발생: {ex.Message}");
//            return new List<FileInfo>();
//        }
//    }

//    // 로컬 파일 업로드
//    private static async Task<List<string>> UploadLocalFiles()
//    {
//        Console.WriteLine($"'{LocalFolder}' 폴더에서 파일을 스캔합니다...");

//        try
//        {
//            var files = GetLocalFiles();
//            var fileIds = new List<string>();

//            foreach (var file in files)
//            {
//                string filePath = file.FullName;
//                string fileName = file.Name;
//                Console.WriteLine($"업로드 중: {fileName}");

//                try
//                {
//                    // OpenAIAssistantsClient의 UploadFileAsync 메서드 사용
//                    var fileResponse = await _assistantsClient.UploadFileAsync(filePath, "assistants");

//                    // 파일 ID 추출
//                    string fileId = JsonSerializer.Deserialize<JsonElement>(fileResponse.ToString()).GetProperty("id").GetString();

//                    if (!string.IsNullOrEmpty(fileId))
//                    {
//                        fileIds.Add(fileId);
//                        // 추적 정보 업데이트
//                        _fileIdMapping[filePath] = fileId;
//                        _deployedFiles[filePath] = file;
//                        Console.WriteLine($"파일 업로드 완료. ID: {fileId}");
//                    }
//                    else
//                    {
//                        Console.WriteLine("파일 ID를 가져올 수 없습니다.");
//                    }
//                }
//                catch (Exception ex)
//                {
//                    Console.WriteLine($"'{fileName}' 파일 업로드 중 오류 발생: {ex.Message}");
//                }
//            }

//            return fileIds;
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"파일 업로드 중 오류 발생: {ex.Message}");
//            throw;
//        }
//    }

//    // Vector Store 생성 및 파일 추가
//    private static async Task<string> CreateVectorStore(List<string> fileIds)
//    {
//        Console.WriteLine("Vector Store 생성 중...");

//        try
//        {
//            // VectorStoreClient 사용하여 Vector Store 생성
//            var request = CreateVectorStoreRequest.Create($"{AssistantName}-VectorStore");
//            request = request.WithFileIds(fileIds);

//            // Vector Store 생성 및 완료될 때까지 대기
//            var vectorStore = await _vectorStoreClient.CreateAndPollVectorStoreAsync(request);
//            string vectorStoreId = vectorStore.Id;

//            Console.WriteLine($"Vector Store가 생성되었습니다. ID: {vectorStoreId}");
//            Console.WriteLine($"상태: {vectorStore.Status}");

//            var fileCounts = vectorStore.GetFileCounts();
//            if (fileCounts != null)
//            {
//                Console.WriteLine($"파일 처리 상태: 완료={fileCounts.Completed}, 실패={fileCounts.Failed}");
//            }

//            return vectorStoreId;
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Vector Store 생성 중 오류 발생: {ex.Message}");
//            throw;
//        }
//    }

//    // 어시스턴트 생성 또는 가져오기
//    private static async Task<string> GetOrCreateAssistant(string vectorStoreId)
//    {
//        Console.WriteLine("어시스턴트 확인 중...");

//        try
//        {
//            // 1. 모든 어시스턴트 가져오기
//            var assistants = await _assistantsClient.ListAssistantsAsync(limit: 100);

//            // 2. 이름으로 어시스턴트 찾기
//            Assistant existingAssistant = null;
//            if (assistants?.Data != null)
//            {
//                existingAssistant = assistants.Data.FirstOrDefault(a => a.Name == AssistantName);
//            }

//            if (existingAssistant != null)
//            {
//                Console.WriteLine($"기존 어시스턴트를 찾았습니다. ID: {existingAssistant.Id}");

//                // 기존 어시스턴트 업데이트 (Vector Store 연결)
//                Console.WriteLine("기존 어시스턴트에 Vector Store 연결 중...");

//                var updateRequest = UpdateAssistantRequest.Create()
//                    .WithTools(new List<Tool> { Tool.CreateFileSearchTool() });

//                var toolResources = AssistantHelpers.CreateFileSearchToolResources(new List<string> { vectorStoreId });
//                updateRequest.WithToolResources(toolResources);

//                await _assistantsClient.UpdateAssistantAsync(existingAssistant.Id, updateRequest);
//                Console.WriteLine("어시스턴트가 성공적으로 업데이트되었습니다.");

//                return existingAssistant.Id;
//            }

//            // 3. 어시스턴트가 없으면 새로 생성
//            Console.WriteLine("어시스턴트를 생성합니다...");

//            var createRequest = CreateAssistantRequest.Create("gpt-4o")
//                .WithName(AssistantName)
//                .WithInstructions("로컬 파일의 내용을 기반으로 질문에 답변하는 어시스턴트입니다.")
//                .WithTools(new List<Tool> { Tool.CreateFileSearchTool() });

//            var newToolResources = AssistantHelpers.CreateFileSearchToolResources(new List<string> { vectorStoreId });
//            createRequest.WithToolResources(newToolResources);

//            var newAssistant = await _assistantsClient.CreateAssistantAsync(createRequest);

//            Console.WriteLine($"새 어시스턴트가 생성되었습니다. ID: {newAssistant.Id}");
//            return newAssistant.Id;
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"GetOrCreateAssistant 메서드에서 오류 발생: {ex.Message}");
//            throw;
//        }
//    }

//    // 테스트 스레드 생성
//    private static async Task<string> CreateTestThread()
//    {
//        Console.WriteLine("테스트 스레드 생성 중...");

//        try
//        {
//            // 초기 메시지 없이 빈 스레드 생성
//            var thread = await _assistantsClient.CreateThreadAsync();
//            Console.WriteLine($"테스트 스레드가 생성되었습니다. ID: {thread.Id}");

//            return thread.Id;
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"스레드 생성 중 오류 발생: {ex.Message}");
//            throw;
//        }
//    }
//}