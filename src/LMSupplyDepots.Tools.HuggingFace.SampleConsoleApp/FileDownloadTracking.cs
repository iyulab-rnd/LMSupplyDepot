using System.Text.Json;
using LMSupplyDepots.Tools.HuggingFace.Download;

namespace LMSupplyDepots.Tools.HuggingFace.SampleConsoleApp;

public class FileDownloadTracking : DownloadTrackingBase
{
    private readonly string _stateFilePath;
    private readonly SemaphoreSlim _lock = new(1);

    public FileDownloadTracking(string stateFilePath)
    {
        _stateFilePath = stateFilePath;
    }

    public async Task ValidateStateFileAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var states = await LoadAllStatesAsync();
            var validStates = new Dictionary<string, ModelFileDownloadState>();
            var removedStates = new List<string>();

            foreach (var kvp in states)
            {
                var state = kvp.Value;
                var outputPath = Path.Combine(GlobalSettings.DataPath, state.ModelId, state.FilePath);

                // 파일 상태 확인
                var (exists, size, isComplete) = await FileVerificationHelper.VerifyExistingFileAsync(
                    outputPath,
                    state.TotalSize);

                if (exists)
                {
                    if (isComplete && state.IsCompleted)
                    {
                        // 완료된 파일이 정상적으로 존재하는 경우
                        validStates[kvp.Key] = state;
                    }
                    else if (size > 0 && size < state.TotalSize)
                    {
                        // 진행 중인 다운로드의 경우 크기 업데이트
                        state.DownloadedSize = size;
                        state.IsCompleted = false;
                        validStates[kvp.Key] = state;
                        removedStates.Add($"{state.FilePath} (Progress: {((double)size / state.TotalSize):P2})");
                    }
                    else if (size > state.TotalSize)
                    {
                        removedStates.Add($"{state.FilePath} (Invalid size: {size} > {state.TotalSize})");
                    }
                }
                else
                {
                    removedStates.Add($"{state.FilePath} (File not found)");
                }
            }

            if (validStates.Count != states.Count)
            {
                using (var fileStream = new FileStream(_stateFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(fileStream))
                {
                    var json = JsonSerializer.Serialize(validStates, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    await writer.WriteAsync(json);
                }

                if (removedStates.Any())
                {
                    Console.WriteLine($"\nIncomplete or invalid downloads detected:");
                    foreach (var state in removedStates)
                    {
                        Console.WriteLine($"- {state}");
                    }
                    Console.WriteLine();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating download state file: {ex.Message}");
            try
            {
                using (var fileStream = new FileStream(_stateFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                using (var writer = new StreamWriter(fileStream))
                {
                    await writer.WriteAsync("{}");
                }
                Console.WriteLine("Download state file has been reset due to validation error.");
            }
            catch
            {
                Console.WriteLine("Failed to reset download state file.");
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    protected override async Task SaveDownloadStateAsync(ModelFileDownloadState state)
    {
        for (int retryCount = 0; retryCount < 3; retryCount++)
        {
            try
            {
                await _lock.WaitAsync();
                var states = await LoadAllStatesAsync();

                var modelId = PathHelper.NormalizeWebPath(state.ModelId);
                var fileName = Path.GetFileName(state.FilePath);
                var key = PathHelper.GetDownloadKey(modelId, fileName);

                state.ModelId = modelId;
                state.FilePath = fileName;
                states[key] = state;

                await SaveStatesToFileAsync(states);
                return;
            }
            catch (IOException) when (retryCount < 2)
            {
                await Task.Delay(100 * (retryCount + 1));
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    private string GetOutputPath(string modelId, string filePath)
    {
        return Path.Combine(GlobalSettings.DataPath, modelId, filePath);
    }

    private async Task SaveStatesToFileAsync(Dictionary<string, ModelFileDownloadState> states)
    {
        using var fileStream = new FileStream(_stateFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        using var writer = new StreamWriter(fileStream);
        var json = JsonSerializer.Serialize(states, new JsonSerializerOptions { WriteIndented = true });
        await writer.WriteAsync(json);
    }

    protected override async Task<ModelFileDownloadState?> LoadDownloadStateAsync(string modelId, string filePath)
    {
        await _lock.WaitAsync();
        try
        {
            var states = await LoadAllStatesAsync();
            var key = PathHelper.GetDownloadKey(modelId, filePath);
            return states.GetValueOrDefault(key);
        }
        finally
        {
            _lock.Release();
        }
    }

    protected override async Task<List<ModelFileDownloadState>> LoadIncompleteDownloadsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var states = await LoadAllStatesAsync();
            return states.Values
                .Where(s => !s.IsCompleted)
                .OrderByDescending(s => s.LastAttempt)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    protected override async Task RemoveCompletedDownloadsAsync()
    {
        await _lock.WaitAsync();
        try
        {
            var states = await LoadAllStatesAsync();
            var incomplete = states
                .Where(kvp => !kvp.Value.IsCompleted)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            if (incomplete.Count != states.Count)
            {
                await SaveStatesToFileAsync(incomplete);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<Dictionary<string, ModelFileDownloadState>> LoadAllStatesAsync()
    {
        if (!File.Exists(_stateFilePath))
        {
            return new Dictionary<string, ModelFileDownloadState>();
        }

        for (int retryCount = 0; retryCount < 3; retryCount++)
        {
            try
            {
                using var fileStream = new FileStream(_stateFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fileStream);
                var json = await reader.ReadToEndAsync();
                return JsonSerializer.Deserialize<Dictionary<string, ModelFileDownloadState>>(json)
                    ?? new Dictionary<string, ModelFileDownloadState>();
            }
            catch (IOException) when (retryCount < 2)
            {
                await Task.Delay(100 * (retryCount + 1));
            }
        }

        Console.WriteLine($"Warning: Unable to load download state after multiple attempts: {_stateFilePath}");
        return new Dictionary<string, ModelFileDownloadState>();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _lock.Dispose();
        }
        base.Dispose(disposing);
    }
}