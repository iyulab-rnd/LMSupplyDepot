# LMSupplyDepots.Tools.HuggingFace

Hugging Face Hub API와 상호 작용하기 위한 .NET 라이브러리입니다. 특히 GGUF 형식의 기계 학습 모델을 다운로드하고 관리하는 데 중점을 두고 있으며, 동시 다운로드, 진행 상황 추적, 다운로드 재개 등의 기능을 제공합니다.

## 주요 기능

- **모델 검색**: 
  - Hugging Face Hub의 모델 검색
  - 다양한 필터링 및 정렬 옵션 지원
  - GGUF 형식 모델 자동 필터링
  
- **다운로드 관리**: 
  - 구성 가능한 동시 다운로드 제한
  - 상세한 진행 상황 추적 및 보고
  - 중단된 다운로드 재개 기능
  - 실패 시 자동 재시도
  - 다운로드 상태 영구 저장

- **파일 관리**:
  - GGUF 모델 파일 특화 지원
  - 파일 무결성 검증
  - 대용량 파일을 위한 효율적인 버퍼 관리

- **오류 처리**:
  - 지수 백오프를 통한 자동 재시도
  - 상세한 오류 보고
  - 인증 오류 감지

## 사용 방법

### 기본 설정

```csharp
// 클라이언트 옵션 생성
var options = new HuggingFaceClientOptions
{
    Token = "your-token", // 선택사항: 비공개 모델에 필요
    MaxConcurrentDownloads = 3,
    ProgressUpdateInterval = 100,
    MaxRetries = 3
};

// 클라이언트 생성
using var client = new HuggingFaceClient(options);
```

### 모델 검색

```csharp
// 모델 검색
var models = await client.SearchModelsAsync(
    search: "llama",
    limit: 5,
    sortField: ModelSortField.Downloads,
    descending: true);

foreach (var model in models)
{
    Console.WriteLine($"모델: {model.ID}");
    Console.WriteLine($"다운로드 수: {model.Downloads}");
}
```

### 파일 다운로드

```csharp
// 특정 파일 다운로드
string repoId = "thebloke/Llama-2-7B-GGUF";
string filePath = "llama-2-7b.Q4_K_M.gguf";
string outputPath = "path/to/output/file.gguf";

await foreach (var progress in client.DownloadFileAsync(repoId, filePath, outputPath))
{
    Console.WriteLine($"진행률: {progress.FormattedProgress}");
    Console.WriteLine($"다운로드 속도: {progress.FormattedDownloadSpeed}");
}
```

### 저장소 전체 다운로드

```csharp
// 저장소의 모든 파일 다운로드
string outputDir = "path/to/output/directory";

await foreach (var progress in client.DownloadRepositoryAsync(repoId, outputDir))
{
    Console.WriteLine($"전체 진행률: {progress.TotalProgress:P0}");
    Console.WriteLine($"완료된 파일: {progress.CompletedFiles.Count}/{progress.TotalFiles.Count}");
}
```

## 설정 옵션

`HuggingFaceClientOptions` 클래스는 다음과 같은 설정 옵션을 제공합니다:

- `Token`: API 인증 토큰
- `MaxConcurrentDownloads` (1-20): 최대 동시 다운로드 수
- `ProgressUpdateInterval` (50-5000ms): 진행 상황 업데이트 간격
- `Timeout` (10s-30min): HTTP 요청 타임아웃
- `BufferSize` (4KB-1MB): 파일 다운로드 버퍼 크기
- `MaxRetries` (0-5): 최대 재시도 횟수
- `RetryDelayMilliseconds` (100-10000ms): 재시도 간 기본 지연 시간

## 오류 처리

라이브러리는 API 관련 오류에 대해 `HuggingFaceException`을 발생시킵니다:

```csharp
try
{
    await client.FindModelByRepoIdAsync("invalid/repo");
}
catch (HuggingFaceException ex)
{
    Console.WriteLine($"오류: {ex.Message}");
    Console.WriteLine($"상태 코드: {ex.StatusCode}");
}
```


## 주요 클래스 및 인터페이스

- `IHuggingFaceClient`: API 상호작용을 위한 핵심 인터페이스
- `HuggingFaceClient`: 주요 클라이언트 구현체
- `HuggingFaceClientOptions`: 클라이언트 구성 옵션
- `FileDownloadProgress`: 파일 다운로드 진행 상황 정보
- `RepoDownloadProgress`: 저장소 다운로드 진행 상황 정보
- `HuggingFaceModel`: 모델 정보 표현
- `ModelFileDownloadState`: 다운로드 상태 추적 정보