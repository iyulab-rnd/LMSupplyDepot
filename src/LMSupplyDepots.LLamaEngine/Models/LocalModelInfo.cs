namespace LMSupplyDepots.LLamaEngine.Models;

public record LocalModelInfo
{
    public string ModelId { get; set; } = null!;
    public string FullPath { get; set; } = null!;
    public LocalModelState State { get; set; } = LocalModelState.Unloaded;
    public string? LastError { get; set; }

    public static bool TryParseIdentifier(string identifier, out (string provider, string modelName, string fileName) result)
    {
        var parts = identifier.Split(['/', ':'], 3);
        if (parts.Length == 3)
        {
            // .gguf 확장자가 있다면 제거
            var fileName = parts[2].EndsWith(".gguf", StringComparison.OrdinalIgnoreCase)
                ? parts[2][..^5]  // .gguf 문자열 길이만큼 제거
                : parts[2];

            result = (parts[0], parts[1], fileName);
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
            ModelId = identifier,
            FullPath = filePath,
            State = LocalModelState.Unloaded
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