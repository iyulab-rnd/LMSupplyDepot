# LMSupplyDepots.Tools.HuggingFace

Hugging Face Hub API�� ��ȣ �ۿ��ϱ� ���� .NET ���̺귯���Դϴ�. Ư�� GGUF ������ ��� �н� ���� �ٿ�ε��ϰ� �����ϴ� �� ������ �ΰ� ������, ���� �ٿ�ε�, ���� ��Ȳ ����, �ٿ�ε� �簳 ���� ����� �����մϴ�.

## �ֿ� ���

- **�� �˻�**: 
  - Hugging Face Hub�� �� �˻�
  - �پ��� ���͸� �� ���� �ɼ� ����
  - GGUF ���� �� �ڵ� ���͸�
  
- **�ٿ�ε� ����**: 
  - ���� ������ ���� �ٿ�ε� ����
  - ���� ���� ��Ȳ ���� �� ����
  - �ߴܵ� �ٿ�ε� �簳 ���
  - ���� �� �ڵ� ��õ�
  - �ٿ�ε� ���� ���� ����

- **���� ����**:
  - GGUF �� ���� Ưȭ ����
  - ���� ���Ἲ ����
  - ��뷮 ������ ���� ȿ������ ���� ����

- **���� ó��**:
  - ���� ������� ���� �ڵ� ��õ�
  - ���� ���� ����
  - ���� ���� ����

## ��� ���

### �⺻ ����

```csharp
// Ŭ���̾�Ʈ �ɼ� ����
var options = new HuggingFaceClientOptions
{
    Token = "your-token", // ���û���: ����� �𵨿� �ʿ�
    MaxConcurrentDownloads = 3,
    ProgressUpdateInterval = 100,
    MaxRetries = 3
};

// Ŭ���̾�Ʈ ����
using var client = new HuggingFaceClient(options);
```

### �� �˻�

```csharp
// �� �˻�
var models = await client.SearchModelsAsync(
    search: "llama",
    limit: 5,
    sortField: ModelSortField.Downloads,
    descending: true);

foreach (var model in models)
{
    Console.WriteLine($"��: {model.ID}");
    Console.WriteLine($"�ٿ�ε� ��: {model.Downloads}");
}
```

### ���� �ٿ�ε�

```csharp
// Ư�� ���� �ٿ�ε�
string repoId = "thebloke/Llama-2-7B-GGUF";
string filePath = "llama-2-7b.Q4_K_M.gguf";
string outputPath = "path/to/output/file.gguf";

await foreach (var progress in client.DownloadFileAsync(repoId, filePath, outputPath))
{
    Console.WriteLine($"�����: {progress.FormattedProgress}");
    Console.WriteLine($"�ٿ�ε� �ӵ�: {progress.FormattedDownloadSpeed}");
}
```

### ����� ��ü �ٿ�ε�

```csharp
// ������� ��� ���� �ٿ�ε�
string outputDir = "path/to/output/directory";

await foreach (var progress in client.DownloadRepositoryAsync(repoId, outputDir))
{
    Console.WriteLine($"��ü �����: {progress.TotalProgress:P0}");
    Console.WriteLine($"�Ϸ�� ����: {progress.CompletedFiles.Count}/{progress.TotalFiles.Count}");
}
```

## ���� �ɼ�

`HuggingFaceClientOptions` Ŭ������ ������ ���� ���� �ɼ��� �����մϴ�:

- `Token`: API ���� ��ū
- `MaxConcurrentDownloads` (1-20): �ִ� ���� �ٿ�ε� ��
- `ProgressUpdateInterval` (50-5000ms): ���� ��Ȳ ������Ʈ ����
- `Timeout` (10s-30min): HTTP ��û Ÿ�Ӿƿ�
- `BufferSize` (4KB-1MB): ���� �ٿ�ε� ���� ũ��
- `MaxRetries` (0-5): �ִ� ��õ� Ƚ��
- `RetryDelayMilliseconds` (100-10000ms): ��õ� �� �⺻ ���� �ð�

## ���� ó��

���̺귯���� API ���� ������ ���� `HuggingFaceException`�� �߻���ŵ�ϴ�:

```csharp
try
{
    await client.FindModelByRepoIdAsync("invalid/repo");
}
catch (HuggingFaceException ex)
{
    Console.WriteLine($"����: {ex.Message}");
    Console.WriteLine($"���� �ڵ�: {ex.StatusCode}");
}
```


## �ֿ� Ŭ���� �� �������̽�

- `IHuggingFaceClient`: API ��ȣ�ۿ��� ���� �ٽ� �������̽�
- `HuggingFaceClient`: �ֿ� Ŭ���̾�Ʈ ����ü
- `HuggingFaceClientOptions`: Ŭ���̾�Ʈ ���� �ɼ�
- `FileDownloadProgress`: ���� �ٿ�ε� ���� ��Ȳ ����
- `RepoDownloadProgress`: ����� �ٿ�ε� ���� ��Ȳ ����
- `HuggingFaceModel`: �� ���� ǥ��
- `ModelFileDownloadState`: �ٿ�ε� ���� ���� ����