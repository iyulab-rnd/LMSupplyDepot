namespace LMSupplyDepots.Tools.HuggingFace.SampleConsoleApp;

public static class PathHelper
{
    public static string GetDownloadKey(string modelId, string fileName)
    {
        modelId = NormalizeWebPath(modelId);
        return $"{modelId}:{fileName}";
    }

    public static string GetRelativePath(string modelId, string filePath)
    {
        // 파일명만 추출
        return Path.GetFileName(filePath);
    }

    public static string GetOutputPath(string baseDir, string modelId, string fileName)
    {
        // 로컬 파일 시스템 경로로 통일
        baseDir = NormalizeFilePath(baseDir);
        modelId = NormalizeFilePath(modelId);

        // baseDir\modelId\filename 구조로 생성
        return Path.Combine(baseDir, modelId, fileName);
    }

    public static string GetModelDirectory(string baseDir, string modelId)
    {
        // 로컬 파일 시스템 경로로 통일
        baseDir = NormalizeFilePath(baseDir);
        modelId = NormalizeFilePath(modelId);

        return Path.Combine(baseDir, modelId);
    }

    public static string GetBaseDirectory(string outputPath)
    {
        // 파일 경로에서 modelId와 파일명을 제외한 기본 디렉토리 추출
        var dir = Path.GetDirectoryName(outputPath) ?? "";
        // modelId 디렉토리를 제외한 상위 경로 반환
        return Path.GetDirectoryName(dir) ?? "";
    }

    public static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        // 로컬 파일 시스템용 경로 정규화
        return NormalizeFilePath(path);
    }

    public static string NormalizeWebPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        // 웹 URL용 정규화 - 항상 정방향 슬래시 사용
        path = path.Replace('\\', '/');

        // 중복된 슬래시 제거
        while (path.Contains("//"))
        {
            path = path.Replace("//", "/");
        }

        return path;
    }

    public static string NormalizeFilePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        // 시스템 구분자로 통일
        path = path.Replace('/', Path.DirectorySeparatorChar)
                  .Replace('\\', Path.DirectorySeparatorChar);

        // 중복된 구분자 제거
        var separator = Path.DirectorySeparatorChar.ToString();
        while (path.Contains(separator + separator))
        {
            path = path.Replace(separator + separator, separator);
        }

        return path;
    }
}