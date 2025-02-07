using LMSupplyDepots.Tools.HuggingFace.Client;
using LMSupplyDepots.Tools.HuggingFace.Common;
using LMSupplyDepots.Tools.HuggingFace.Download;
using LMSupplyDepots.Tools.HuggingFace.Models;
using System.Net;

namespace LMSupplyDepots.Tools.HuggingFace.SampleConsoleApp;

internal class SampleDownloadModel
{
    private static readonly ManualResetEventSlim _pauseEvent = new(true);

    private static string CreateProgressBar(double progress, int width = 50)
    {
        int filled = (int)(progress * width);
        int empty = width - filled;
        return $"[{new string('#', filled)}{new string('-', empty)}]";
    }

    private static string CreateSpeedIndicator(string speed, int width = 15)
    {
        return speed.PadLeft(width);
    }

    private static void UpdateProgress(double totalProgress, int totalFiles, int completedFiles, FileDownloadProgress? currentFile)
    {
        Console.SetCursorPosition(0, Console.CursorTop);

        // 전체 진행률 표시
        var progressBar = CreateProgressBar(totalProgress);
        Console.Write($"\rTotal Progress: {progressBar} {(totalProgress * 100):F0}% ({completedFiles}/{totalFiles} files)");

        // 현재 다운로드 중인 파일이 있으면 표시
        if (currentFile != null)
        {
            var fileName = Path.GetFileName(currentFile.UploadPath);
            var fileProgress = currentFile.DownloadProgress ?? 0;
            var fileProgressBar = CreateProgressBar(fileProgress);
            var speedIndicator = CreateSpeedIndicator(currentFile.FormattedDownloadSpeed);

            Console.WriteLine();
            Console.Write($"  {fileName}: {fileProgressBar} {(fileProgress * 100):F0}% {speedIndicator}");
        }

        // 커서를 시작 위치로 되돌림
        Console.SetCursorPosition(0, Console.CursorTop - (currentFile != null ? 1 : 0));
    }

    public static async Task RunAsync(HuggingFaceClient client)
    {
        Console.WriteLine("\nModel Download Sample");
        Console.WriteLine("--------------------");
        Console.WriteLine("Press 'P' to pause/resume download");
        Console.WriteLine("Press 'Q' to quit download");

        Console.Write("\nEnter model ID (e.g. provider/modelName): ");
        var modelId = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(modelId))
        {
            Console.WriteLine("Model ID is required.");
            return;
        }

        // 다운로드 옵션 선택
        Console.WriteLine("\nSelect download option:");
        Console.WriteLine("1. Model weights only");
        Console.WriteLine("2. Essential files (weights, config, tokenizer)");
        Console.WriteLine("3. All files");

        var option = Console.ReadLine();

        var outputDir = GlobalSettings.DataPath;

        if (!Directory.Exists(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }

        using var cts = new CancellationTokenSource();

        // 키 입력 모니터링을 위한 태스크
        var keyMonitorTask = Task.Run(() =>
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
                                _pauseEvent.Reset(); // 일시정지
                                Console.WriteLine("\nDownload paused. Press 'P' to resume.");
                            }
                            else
                            {
                                _pauseEvent.Set(); // 재개
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

        try
        {
            var model = await client.FindModelByRepoIdAsync(modelId, cts.Token);
            Console.WriteLine($"\nFound model: {model.ID}");

            string[] filesToDownload;
            IAsyncEnumerable<RepoDownloadProgress> downloadOperation;

            switch (option)
            {
                case "1":
                    filesToDownload = model.GetModelWeightPaths();
                    Console.WriteLine("Downloading model weights only...");
                    break;
                case "2":
                    filesToDownload = model.GetEssentialModelPaths();
                    Console.WriteLine("Downloading essential model files...");
                    break;
                default:
                    filesToDownload = model.GetFilePaths();
                    Console.WriteLine("Downloading all files...");
                    break;
            }

            Console.WriteLine($"Files to download: {filesToDownload.Length}");
            Console.WriteLine("Files:");
            foreach (var file in filesToDownload)
            {
                Console.WriteLine($"  - {file}");
            }
            Console.WriteLine($"Download path: {outputDir}\n");

            downloadOperation = option == "3"
                ? client.DownloadRepositoryAsync(modelId, outputDir)
                : client.DownloadRepositoryFilesAsync(modelId, filesToDownload, outputDir);

            await foreach (var progress in downloadOperation)
            {
                if (cts.Token.IsCancellationRequested)
                    break;

                await _pauseEvent.WaitHandle.WaitOneAsync(cts.Token);

                var currentProgress = progress.CurrentProgresses.FirstOrDefault();
                UpdateProgress(
                    progress.TotalProgress,
                    progress.TotalFiles.Count,
                    progress.CompletedFiles.Count,
                    currentProgress
                );
            }

            Console.WriteLine("\n"); // 진행률 표시 후 새 줄 추가
            if (cts.Token.IsCancellationRequested)
                Console.WriteLine("Download cancelled by user.");
            else
                Console.WriteLine("Download completed successfully!");
        }
        catch (HuggingFaceException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
        {
            Console.WriteLine($"\nError: {ex.Message}");
            Console.WriteLine("You can set your API token when starting the application.");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\nError: {ex.Message}");
        }
        finally
        {
            cts.Cancel(); // 키 모니터링 태스크 종료
            await keyMonitorTask;
            _pauseEvent.Set(); // 이벤트 리셋
        }
    }
}