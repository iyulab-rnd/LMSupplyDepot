using LMSupplyDepots.Tools.HuggingFace.Download;
namespace LMSupplyDepots.Tools.HuggingFace.SampleConsoleApp;

public class ConsoleOutputManager
{
    public static void WriteProgressUpdate(double totalProgress, int totalFiles, int completedFiles, FileDownloadProgress? currentFile)
    {
        Console.SetCursorPosition(0, Console.CursorTop);

        var progressBar = CreateProgressBar(totalProgress);
        Console.Write($"\rTotal Progress: {progressBar} {(totalProgress * 100):F0}% ({completedFiles}/{totalFiles} files)");

        if (currentFile != null)
        {
            var fileName = Path.GetFileName(currentFile.UploadPath);
            var fileProgress = currentFile.DownloadProgress ?? 0;
            var fileProgressBar = CreateProgressBar(fileProgress);
            var speedIndicator = CreateSpeedIndicator(currentFile.FormattedDownloadSpeed);

            Console.WriteLine();
            Console.Write($"  {fileName}: {fileProgressBar} {(fileProgress * 100):F0}% {speedIndicator}");
        }

        Console.SetCursorPosition(0, Console.CursorTop - (currentFile != null ? 1 : 0));
    }

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
}