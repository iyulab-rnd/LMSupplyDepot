using LMSupplyDepots.Tools.HuggingFace.Client;
using LMSupplyDepots.Tools.HuggingFace.Common;
using LMSupplyDepots.Tools.HuggingFace.Download;
using LMSupplyDepots.Tools.HuggingFace.Models;
using System.Net;

namespace LMSupplyDepots.Tools.HuggingFace.SampleConsoleApp;

internal class SampleDownloadModel
{
    private static readonly ManualResetEventSlim _pauseEvent = new(true);
    private static readonly string _downloadStateFile = Path.Combine(
        Path.GetDirectoryName(typeof(SampleDownloadModel).Assembly.Location) ?? "",
        "download_state.json");

    public static async Task RunAsync(HuggingFaceClient client)
    {
        Console.WriteLine("\nModel Download Sample");
        Console.WriteLine("--------------------");

        using var downloadTracking = new FileDownloadTracking(_downloadStateFile);
        await downloadTracking.ValidateStateFileAsync();
        var incompleteDownloads = await downloadTracking.GetIncompleteDownloadsAsync();

        if (incompleteDownloads.Any())
        {
            await HandleIncompleteDownloads(client, downloadTracking, incompleteDownloads);
            return;
        }

        Console.Write("\nEnter model ID (e.g. provider/modelName): ");
        var modelId = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(modelId))
        {
            Console.WriteLine("Model ID is required.");
            return;
        }

        try
        {
            var model = await client.FindModelByRepoIdAsync(modelId);
            Console.WriteLine($"\nFound model: {model.ModelId}");

            // Get list of GGUF files with their sizes
            var ggufFiles = model.GetGgufModelPaths();
            if (!ggufFiles.Any())
            {
                Console.WriteLine("No GGUF files found in this model repository.");
                return;
            }

            var fileInfoTasks = ggufFiles.Select(async filePath =>
            {
                try
                {
                    var fileInfo = await client.GetFileInfoAsync(modelId, filePath);
                    return (Path: filePath, Size: fileInfo.Size);
                }
                catch
                {
                    return (Path: filePath, Size: (long?)null);
                }
            });

            var fileInfos = await Task.WhenAll(fileInfoTasks);

            // Display files with sizes
            Console.WriteLine("\nAvailable GGUF files:");
            for (int i = 0; i < fileInfos.Length; i++)
            {
                var sizeStr = fileInfos[i].Size.HasValue
                    ? StringFormatter.FormatSize(fileInfos[i].Size.Value)
                    : "Size unknown";
                Console.WriteLine($"{i + 1}. {fileInfos[i].Path} ({sizeStr})");
            }

            // Let user select files
            Console.WriteLine("\nEnter file numbers to download (comma-separated, or 'all' for all files):");
            var selection = Console.ReadLine()?.Trim().ToLower();

            var selectedFiles = new List<(string Path, long? Size)>();
            if (selection == "all")
            {
                selectedFiles.AddRange(fileInfos);
            }
            else if (!string.IsNullOrWhiteSpace(selection))
            {
                var selectedIndices = selection.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => int.TryParse(s, out var _))
                    .Select(s => int.Parse(s) - 1);

                foreach (var index in selectedIndices)
                {
                    if (index >= 0 && index < fileInfos.Length)
                    {
                        selectedFiles.Add(fileInfos[index]);
                    }
                }
            }

            if (!selectedFiles.Any())
            {
                Console.WriteLine("No valid files selected.");
                return;
            }

            var outputDir = GlobalSettings.DataPath;
            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            // Register event handlers
            downloadTracking.DownloadStarted += (s, e) =>
                Console.WriteLine($"\nStarting download: {e.FilePath}");

            downloadTracking.DownloadProgressUpdated += (s, e) =>
                ConsoleOutputManager.WriteProgressUpdate(
                    e.Progress,
                    selectedFiles.Count,
                    selectedFiles.Count(f =>
                        downloadTracking.GetResumePositionAsync(modelId, f.Path).Result == f.Size),
                    new FileDownloadProgress
                    {
                        UploadPath = Path.Combine(outputDir, e.ModelId, e.FilePath),  // 여기서 경로 생성
                        CurrentBytes = e.DownloadedSize,
                        TotalBytes = e.TotalSize,
                        DownloadProgress = e.Progress
                    });


            downloadTracking.DownloadCompleted += (s, e) =>
                Console.WriteLine($"\nCompleted download: {e.FilePath}");

            await StartDownload(client, modelId, outputDir, selectedFiles, downloadTracking);
        }
        catch (Exception ex)
        {
            HandleError(ex);
        }
    }

    private static async Task HandleIncompleteDownloads(
        HuggingFaceClient client,
        FileDownloadTracking downloadTracking,
        IReadOnlyList<ModelFileDownloadState> incomplete)
    {
        Console.WriteLine($"\nFound {incomplete.Count} incomplete downloads:");
        for (int i = 0; i < incomplete.Count; i++)
        {
            var file = incomplete[i];
            Console.WriteLine($"{i + 1}. {file.FilePath} ({file.Progress:P1} completed)");
        }

        Console.WriteLine("\nWhat would you like to do?");
        Console.WriteLine("1. Resume downloads");
        Console.WriteLine("2. Start fresh");
        Console.WriteLine("3. Cancel and return to menu");

        var choice = Console.ReadLine()?.Trim();
        switch (choice)
        {
            case "1":
                await ResumeDownloads(client, downloadTracking, incomplete);
                break;
            case "2":
                await downloadTracking.CleanupCompletedDownloadsAsync();
                Console.WriteLine("Previous download state cleared. Please start a new download.");
                break;
            default:
                Console.WriteLine("Returning to main menu.");
                break;
        }
    }

    private static async Task StartDownload(
        HuggingFaceClient client,
        string modelId,
        string outputDir,
        List<(string Path, long? Size)> selectedFiles,
        FileDownloadTracking downloadTracking)
    {
        Console.WriteLine($"\nStarting download of {selectedFiles.Count} files to {outputDir}");

        // 모델 ID는 항상 웹 경로 형식으로 정규화 (이 시점에서 한 번만)
        modelId = PathHelper.NormalizeWebPath(modelId);

        // 출력 디렉토리는 로컬 파일 시스템 경로로 정규화
        outputDir = Path.GetFullPath(outputDir);

        Console.WriteLine($"Using model ID: {modelId}");
        Console.WriteLine($"Output directory: {outputDir}");

        // 기존 파일 확인
        var existingFiles = await FileVerificationHelper.VerifyExistingDownloadsAsync(
            modelId, selectedFiles, outputDir);

        if (existingFiles.Any())
        {
            Console.WriteLine("\nFound existing partially downloaded files:");
            foreach (var (filePath, (fileSize, isComplete)) in existingFiles)
            {
                var totalSize = selectedFiles.First(f => f.Path == filePath).Size ?? 0;
                var percentage = ((double)fileSize / totalSize * 100).ToString("F1");
                Console.WriteLine($"  - {filePath}: {percentage}% downloaded");
            }

            Console.WriteLine("\nWould you like to resume these downloads? (Y/N)");
            var answer = Console.ReadLine()?.Trim().ToUpper();

            if (answer != "Y")
            {
                Console.WriteLine("Starting fresh downloads...");
                existingFiles.Clear();
            }
            else
            {
                Console.WriteLine("Resuming downloads...");
            }
        }

        Console.WriteLine("\nPress 'P' to pause/resume download");
        Console.WriteLine("Press 'Q' to quit download\n");

        using var cts = new CancellationTokenSource();
        var keyMonitorTask = StartKeyMonitoring(cts);

        try
        {
            foreach (var file in selectedFiles)
            {
                if (cts.Token.IsCancellationRequested) break;

                // 파일명만 추출하여 사용
                var fileName = Path.GetFileName(file.Path);
                // 출력 경로 구성 (이 시점에서 정규화)
                var outputPath = Path.Combine(outputDir, modelId, fileName);

                // 디렉토리가 없다면 생성
                var directoryPath = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                if (!file.Size.HasValue)
                {
                    Console.WriteLine($"Skipping {fileName} - size unknown");
                    continue;
                }

                // 이어받기 위치 결정
                long resumePosition = 0;
                if (existingFiles.TryGetValue(file.Path, out var existingSize))
                {
                    resumePosition = existingSize.Size;
                }
                else
                {
                    resumePosition = await downloadTracking.GetResumePositionAsync(modelId, fileName);
                }

                if (resumePosition >= file.Size.Value)
                {
                    Console.WriteLine($"File already completed: {fileName}");
                    continue;
                }

                try
                {
                    // 다운로드 시작 시 상태 객체를 올바른 형식으로 생성
                    await downloadTracking.RecordDownloadStartAsync(
                        modelId,     // 이미 정규화된 모델 ID
                        fileName,    // 순수 파일명
                        file.Size.Value);

                    if (resumePosition > 0)
                    {
                        Console.WriteLine($"Resuming {fileName} from {StringFormatter.FormatSize(resumePosition)}");
                    }

                    // 파일 다운로드 스트림 처리
                    await foreach (var progress in client.DownloadFileAsync(
                        modelId,
                        file.Path,    // 원본 파일 경로
                        outputPath,   // 전체 경로
                        resumePosition,
                        cts.Token))
                    {
                        if (cts.Token.IsCancellationRequested) break;
                        await _pauseEvent.WaitHandle.WaitOneAsync(cts.Token);

                        // 진행상황 업데이트
                        await downloadTracking.UpdateProgressAsync(
                            modelId,
                            fileName,    // 순수 파일명
                            progress.CurrentBytes,
                            progress.IsCompleted);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\nError downloading {fileName}: {ex.Message}");
                    Console.WriteLine("Continuing with next file...");
                    continue;
                }
            }

            if (!cts.Token.IsCancellationRequested)
            {
                Console.WriteLine("\nAll downloads completed successfully!");
                await downloadTracking.CleanupCompletedDownloadsAsync();
            }
            else
            {
                Console.WriteLine("\nDownload cancelled by user. Progress has been saved.");
            }
        }
        finally
        {
            cts.Cancel();
            await keyMonitorTask;
            _pauseEvent.Set();
        }
    }

    private static async Task ResumeDownloads(
        HuggingFaceClient client,
        FileDownloadTracking downloadTracking,
        IReadOnlyList<ModelFileDownloadState> incomplete)
    {
        var firstState = incomplete[0];

        // 모델 ID는 웹 경로 형식으로 정규화
        var modelId = PathHelper.NormalizeWebPath(firstState.ModelId);

        // DataPath 기준 상대 경로로 baseDir 설정
        var baseDir = GlobalSettings.DataPath;

        var selectedFiles = incomplete
            .Select(i => (Path: Path.GetFileName(i.FilePath), Size: (long?)i.TotalSize))
            .ToList();

        // 이벤트 핸들러 등록
        downloadTracking.DownloadProgressUpdated += (s, e) =>
            ConsoleOutputManager.WriteProgressUpdate(
                e.Progress,
                selectedFiles.Count,
                selectedFiles.Count(f =>
                    downloadTracking.GetResumePositionAsync(modelId, f.Path).Result == f.Size),
                new FileDownloadProgress
                {
                    UploadPath = Path.Combine(baseDir, e.ModelId, e.FilePath),  // 여기서 경로 생성
                    CurrentBytes = e.DownloadedSize,
                    TotalBytes = e.TotalSize,
                    DownloadProgress = e.Progress
                });

        downloadTracking.DownloadStarted += (s, e) =>
            Console.WriteLine($"\nStarting download: {e.FilePath}");

        downloadTracking.DownloadCompleted += (s, e) =>
            Console.WriteLine($"\nCompleted download: {e.FilePath}");

        await StartDownload(
            client,
            modelId,       // 정규화된 모델 ID 전달
            baseDir,       // DataPath 전달
            selectedFiles, // 순수 파일명과 크기만 포함된 리스트
            downloadTracking);
    }

    private static Task StartKeyMonitoring(CancellationTokenSource cts)
    {
        return Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.P:
                            if (_pauseEvent.IsSet)
                            {
                                _pauseEvent.Reset();
                                Console.WriteLine("\nDownload paused. Press 'P' to resume.");
                            }
                            else
                            {
                                _pauseEvent.Set();
                                Console.WriteLine("\nDownload resumed.");
                            }
                            break;
                        case ConsoleKey.Q:
                            cts.Cancel();
                            return;
                    }
                }
                Thread.Sleep(100);
            }
        });
    }

    private static void HandleError(Exception ex)
    {
        if (ex is HuggingFaceException hfEx && hfEx.StatusCode == HttpStatusCode.Unauthorized)
        {
            Console.WriteLine($"\nError: {ex.Message}");
            Console.WriteLine("You can set your API token when starting the application.");
        }
        else
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
    }
}