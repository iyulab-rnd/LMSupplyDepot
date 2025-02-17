namespace LMSupplyDepots.LLamaEngine.Models;

public record LocalModelInfo
{
    public required string Provider { get; init; }
    public required string ModelName { get; init; }
    public required string FileName { get; init; }
    public required string FullPath { get; init; }
    public LocalModelState State { get; set; } = LocalModelState.Unloaded;
    public string? LastError { get; set; }

    public string GetFullIdentifier() => $"{Provider}/{ModelName}:{FileName}";

    public static bool TryParseIdentifier(string identifier, out (string provider, string modelName, string fileName) result)
    {
        var parts = identifier.Split(['/', ':'], 3);
        if (parts.Length == 3)
        {
            result = (parts[0], parts[1], parts[2]);
            return true;
        }

        result = default;
        return false;
    }

    public static LocalModelInfo CreateFromIdentifier(string filePath, string identifier)
    {
        if (!TryParseIdentifier(identifier, out var parts))
        {
            throw new ArgumentException($"Invalid model identifier format: {identifier}");
        }

        return new LocalModelInfo
        {
            Provider = parts.provider,
            ModelName = parts.modelName,
            FileName = parts.fileName,
            FullPath = filePath
        };
    }
}

public enum LocalModelState
{
    Unloaded,   // 초기 상태 또는 언로드된 상태
    Loading,    // 로드 중
    Loaded,     // 로드 완료
    Failed,     // 로드 또는 작업 실패
    Unloading   // 언로드 중
}