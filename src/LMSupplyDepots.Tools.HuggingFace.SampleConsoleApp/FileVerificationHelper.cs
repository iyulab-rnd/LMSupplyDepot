using LMSupplyDepots.Tools.HuggingFace.Common;

namespace LMSupplyDepots.Tools.HuggingFace.SampleConsoleApp;

internal static class FileVerificationHelper
{
    public static async Task<(bool exists, long size, bool isComplete)> VerifyExistingFileAsync(
        string filePath,
        long? expectedSize = null)
    {
        try
        {
            // 경로 정규화 (중복 제거)
            filePath = Path.GetFullPath(filePath);

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File does not exist: {filePath}");
                return (false, 0, false);
            }

            var fileInfo = new FileInfo(filePath);

            try
            {
                using var stream = new FileStream(
                    filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite);

                var actualSize = fileInfo.Length;
                var isComplete = expectedSize.HasValue && actualSize == expectedSize.Value;

                return (true, actualSize, isComplete);
            }
            catch (IOException ex)
            {
                Console.WriteLine($"File access error: {filePath}");
                Console.WriteLine($"Error details: {ex.Message}");
                return (true, fileInfo.Length, false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Verification error: {filePath}");
            Console.WriteLine($"Error details: {ex.Message}");
            return (false, 0, false);
        }
    }

    public static async Task<Dictionary<string, (long Size, bool IsComplete)>> VerifyExistingDownloadsAsync(
        string modelId,
        IEnumerable<(string Path, long? Size)> files,
        string outputDir)
    {
        var result = new Dictionary<string, (long Size, bool IsComplete)>();

        outputDir = Path.GetFullPath(outputDir); // 전체 경로로 변환

        Console.WriteLine($"Base output directory: {outputDir}");
        Console.WriteLine($"Model ID: {modelId}");

        foreach (var file in files)
        {
            if (!file.Size.HasValue) continue;

            var fileName = Path.GetFileName(file.Path);
            // modelId를 한 번만 사용하여 경로 구성
            var fullFilePath = Path.Combine(outputDir, modelId, fileName);

            Console.WriteLine($"Checking file: {fullFilePath}");

            var (exists, size, isComplete) = await VerifyExistingFileAsync(fullFilePath, file.Size.Value);

            if (exists && size > 0)
            {
                result[file.Path] = (size, isComplete);
                Console.WriteLine($"Found existing file: {fullFilePath} ({StringFormatter.FormatSize(size)})");
            }
            else
            {
                Console.WriteLine($"File not found or empty: {fullFilePath}");
            }
        }

        return result;
    }
}