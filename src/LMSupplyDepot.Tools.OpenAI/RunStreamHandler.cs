namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Handles streaming of runs using Server-Sent Events (SSE)
/// </summary>
public class RunStreamHandler : IDisposable
{
    private readonly string _threadId;
    private readonly string _assistantId;
    private readonly string _apiKey;
    private HttpClient _httpClient;
    private CancellationTokenSource _cts;
    private bool _isStarted = false;
    private bool _disposed = false;
    private Stream _stream;
    private StreamReader _reader;

    /// <summary>
    /// Event that is triggered when data is received
    /// </summary>
    public event Action<StreamEvent> OnData;

    /// <summary>
    /// Event that is triggered when an error occurs
    /// </summary>
    public event Action<Exception> OnError;

    /// <summary>
    /// Event that is triggered when the stream is complete
    /// </summary>
    public event Action OnComplete;

    /// <summary>
    /// Creates a new RunStreamHandler for SSE streaming
    /// </summary>
    internal RunStreamHandler(
        OpenAIAssistantsClient client,
        string apiKey,
        string threadId,
        string assistantId)
    {
        _threadId = threadId ?? throw new ArgumentNullException(nameof(threadId));
        _assistantId = assistantId ?? throw new ArgumentNullException(nameof(assistantId));
        _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
        _httpClient = new HttpClient();
        _cts = new CancellationTokenSource();

        // API 키 헤더 설정
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        // OpenAI-Beta 헤더 설정
        _httpClient.DefaultRequestHeaders.Add("OpenAI-Beta", "assistants=v2");

        Debug.WriteLine($"[RunStreamHandler] Created for thread {threadId} with assistant {assistantId}");
    }

    /// <summary>
    /// Starts the streaming process using SSE
    /// </summary>
    public async Task StartStreamingAsync()
    {
        if (_isStarted)
            throw new InvalidOperationException("Streaming has already been started.");

        Debug.WriteLine($"[RunStreamHandler] Starting streaming for thread {_threadId}");
        _isStarted = true;

        try
        {
            // 스트리밍 모드로 런을 생성하고 응답을 바로 스트림으로 처리
            var url = $"https://api.openai.com/v1/threads/{_threadId}/runs";

            var content = new
            {
                assistant_id = _assistantId,
                stream = true // 스트리밍 활성화
            };

            var jsonContent = JsonSerializer.Serialize(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var request = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(jsonContent, Encoding.UTF8, "application/json")
            };

            // SSE 수신을 위한 Accept 헤더 설정
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/event-stream"));

            // API 키 및 베타 헤더 확인
            if (request.Headers.Authorization == null)
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
            }

            if (!request.Headers.Contains("OpenAI-Beta"))
            {
                request.Headers.Add("OpenAI-Beta", "assistants=v2");
            }

            Debug.WriteLine($"[RunStreamHandler] Creating streaming run for thread: {_threadId}");

            // 응답을 스트림으로 받기
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, _cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"[RunStreamHandler] Error creating streaming run: {response.StatusCode}, {errorContent}");
                throw new Exception($"API request failed: {response.StatusCode}, {errorContent}");
            }

            // 응답에서 스트림 가져오기
            _stream = await response.Content.ReadAsStreamAsync();
            _reader = new StreamReader(_stream);

            // 스트림 처리 시작
            await ProcessStreamAsync();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.WriteLine($"[RunStreamHandler] Error during streaming: {ex.Message}");
            if (ex.InnerException != null)
            {
                Debug.WriteLine($"[RunStreamHandler] Inner exception: {ex.InnerException.Message}");
            }
            TriggerErrorEvent(ex);
            throw;
        }
    }

    /// <summary>
    /// Processes the SSE stream from OpenAI
    /// </summary>
    private async Task ProcessStreamAsync()
    {
        Debug.WriteLine("[RunStreamHandler] Processing SSE stream");

        try
        {
            string line;
            StringBuilder eventData = new StringBuilder();
            string eventName = null;

            // 스트림을 한 줄씩 읽기
            while (!_cts.Token.IsCancellationRequested && !_reader.EndOfStream)
            {
                line = await _reader.ReadLineAsync();
                Debug.WriteLine($"[RunStreamHandler] Raw line: {line}");

                // 빈 줄은 이벤트의 끝을 의미
                if (string.IsNullOrEmpty(line))
                {
                    if (eventData.Length > 0 && !string.IsNullOrEmpty(eventName))
                    {
                        // 완전한 이벤트 처리
                        ProcessEvent(eventName, eventData.ToString().Trim());
                        eventData.Clear();
                        eventName = null;
                    }
                    continue;
                }

                // SSE 라인 파싱
                if (line.StartsWith("event:"))
                {
                    eventName = line.Substring(6).Trim();
                }
                else if (line.StartsWith("data:"))
                {
                    var data = line.Substring(5).Trim();

                    // "[DONE]" 특수 마커 체크
                    if (data == "[DONE]")
                    {
                        TriggerEvent(new StreamEvent
                        {
                            Event = "done",
                            Data = new JsonElement()
                        });
                        TriggerCompleteEvent();
                        break;
                    }

                    // 데이터 누적
                    eventData.AppendLine(data);
                }
            }

            Debug.WriteLine("[RunStreamHandler] SSE stream ended");
            TriggerCompleteEvent();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Debug.WriteLine($"[RunStreamHandler] Error processing stream: {ex.Message}");
            TriggerErrorEvent(ex);
        }
    }

    /// <summary>
    /// Processes an individual SSE event
    /// </summary>
    private void ProcessEvent(string eventName, string eventDataRaw)
    {
        try
        {
            Debug.WriteLine($"[RunStreamHandler] Processing event: {eventName}, Data: {eventDataRaw}");

            // 데이터가 유효한지 확인
            if (string.IsNullOrEmpty(eventDataRaw))
            {
                Debug.WriteLine("[RunStreamHandler] Empty event data, skipping");
                return;
            }

            JsonElement eventData;
            try
            {
                // JSON 데이터 파싱 시도
                eventData = JsonSerializer.Deserialize<JsonElement>(eventDataRaw, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
            }
            catch (JsonException ex)
            {
                Debug.WriteLine($"[RunStreamHandler] JSON parsing error: {ex.Message}");
                Debug.WriteLine($"[RunStreamHandler] Raw event data: {eventDataRaw}");

                // JSON 파싱 실패 시 빈 객체 생성
                eventData = JsonDocument.Parse("{}").RootElement;
            }

            // StreamEvent 생성 및 트리거
            var streamEvent = new StreamEvent
            {
                Event = eventName,
                Data = eventData
            };

            TriggerEvent(streamEvent);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RunStreamHandler] Error processing event: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to trigger an event
    /// </summary>
    private void TriggerEvent(StreamEvent streamEvent)
    {
        try
        {
            OnData?.Invoke(streamEvent);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RunStreamHandler] Error in OnData handler: {ex.Message}");
        }
    }

    /// <summary>
    /// Helper method to trigger an error event
    /// </summary>
    private void TriggerErrorEvent(Exception ex)
    {
        try
        {
            OnError?.Invoke(ex);
        }
        catch (Exception handlerEx)
        {
            Debug.WriteLine($"[RunStreamHandler] Error in OnError handler: {handlerEx.Message}");
        }
    }

    /// <summary>
    /// Helper method to trigger the complete event
    /// </summary>
    private void TriggerCompleteEvent()
    {
        try
        {
            Debug.WriteLine("[RunStreamHandler] Triggering OnComplete event");
            OnComplete?.Invoke();
            Debug.WriteLine("[RunStreamHandler] OnComplete event triggered successfully");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[RunStreamHandler] Error in OnComplete handler: {ex.Message}");
        }
    }

    /// <summary>
    /// Stops the streaming
    /// </summary>
    public void StopStreaming()
    {
        if (!_disposed)
        {
            Debug.WriteLine("[RunStreamHandler] Stopping stream");
            _cts.Cancel();
        }
    }

    /// <summary>
    /// Disposes the resources
    /// </summary>
    public void Dispose()
    {
        if (!_disposed)
        {
            Debug.WriteLine("[RunStreamHandler] Disposing resources");

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _reader?.Dispose();
            _reader = null;

            _stream?.Dispose();
            _stream = null;

            _httpClient?.Dispose();
            _httpClient = null;

            _disposed = true;
        }
    }
}