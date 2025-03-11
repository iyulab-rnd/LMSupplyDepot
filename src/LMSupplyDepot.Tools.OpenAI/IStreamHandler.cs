namespace LMSupplyDepot.Tools.OpenAI;

/// <summary>
/// Interface for stream handlers that process server-sent events
/// </summary>
/// <typeparam name="T">The type of data that will be emitted by the handler</typeparam>
public interface IStreamHandler<T> : IDisposable
{
    /// <summary>
    /// Stops the current streaming operation
    /// </summary>
    void StopStreaming();

    /// <summary>
    /// Event that is triggered when data is received
    /// </summary>
    event Action<T> OnData;

    /// <summary>
    /// Event that is triggered when an error occurs
    /// </summary>
    event Action<Exception> OnError;

    /// <summary>
    /// Event that is triggered when the stream is complete
    /// </summary>
    event Action OnComplete;
}

/// <summary>
/// Handler for streaming API responses
/// </summary>
public class StreamHandler<T> : IStreamHandler<T>, IDisposable
{
    private CancellationTokenSource _cts;
    private readonly HttpResponseMessage _response;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;
    private bool _completeFired;
    private readonly object _completeLock = new object();
    private Stream _stream;
    private StreamReader _reader;

    /// <summary>
    /// Event triggered when data is received
    /// </summary>
    public event Action<T> OnData;

    /// <summary>
    /// Event triggered when an error occurs
    /// </summary>
    public event Action<Exception> OnError;

    /// <summary>
    /// Event triggered when the stream is complete
    /// </summary>
    public event Action OnComplete;

    /// <summary>
    /// Initializes a new instance of the stream handler
    /// </summary>
    public StreamHandler(HttpResponseMessage response)
    {
        _response = response ?? throw new ArgumentNullException(nameof(response));
        _cts = new CancellationTokenSource();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        // Start processing in background immediately
        Debug.WriteLine("[StreamHandler] Initializing and starting stream processing");
        _ = ProcessStreamAsync();
    }

    /// <summary>
    /// Processes the stream asynchronously
    /// </summary>
    private async Task ProcessStreamAsync()
    {
        try
        {
            Debug.WriteLine("[StreamHandler] Starting to process stream");
            _stream = await _response.Content.ReadAsStreamAsync();
            _reader = new StreamReader(_stream);

            string line;
            StringBuilder eventData = new StringBuilder();
            string eventName = null;

            while (!_reader.EndOfStream && !_cts.Token.IsCancellationRequested)
            {
                line = await _reader.ReadLineAsync();
                Debug.WriteLine($"[StreamHandler] Raw stream line: {line}");

                if (string.IsNullOrEmpty(line))
                {
                    // 빈 줄은 이벤트의 끝을 의미
                    if (eventData.Length > 0 && eventName != null)
                    {
                        // SSE 이벤트 처리
                        ProcessSSEEvent(eventName, eventData.ToString().Trim());
                        eventData.Clear();
                        eventName = null;
                    }
                    continue;
                }

                // SSE 형식 파싱
                if (line.StartsWith("event:"))
                {
                    eventName = line.Substring(6).Trim();
                }
                else if (line.StartsWith("data:"))
                {
                    var data = line.Substring(5).Trim();

                    if (data == "[DONE]")
                    {
                        Debug.WriteLine("[StreamHandler] [DONE] marker received");
                        FireComplete();
                        break;
                    }

                    eventData.AppendLine(data);
                }
            }

            // Ensure we trigger OnComplete if not already done
            if (!_cts.Token.IsCancellationRequested)
            {
                Debug.WriteLine("[StreamHandler] End of stream reached");
                FireComplete();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StreamHandler] Stream processing error: {ex.Message}");
            InvokeOnError(ex);
        }
    }

    /// <summary>
    /// Processes a Server-Sent Event
    /// </summary>
    private void ProcessSSEEvent(string eventName, string eventData)
    {
        Debug.WriteLine($"[StreamHandler] Processing SSE event: {eventName}");

        try
        {
            if (typeof(T) == typeof(string))
            {
                // 문자열 타입인 경우 그대로 이벤트 발생
                InvokeOnData((T)(object)eventData);
                return;
            }

            // JSON 파싱 시도
            var item = JsonSerializer.Deserialize<T>(eventData, _jsonOptions);
            if (item != null)
            {
                InvokeOnData(item);
            }
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"[StreamHandler] JSON parsing error: {ex.Message}");
            Debug.WriteLine($"[StreamHandler] Raw event data: {eventData}");
            InvokeOnError(new Exception($"JSON parsing error: {ex.Message}", ex));
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StreamHandler] Error processing event: {ex.Message}");
            InvokeOnError(ex);
        }
    }

    private void InvokeOnData(T data)
    {
        try
        {
            OnData?.Invoke(data);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[StreamHandler] Error in OnData handler: {ex.Message}");
        }
    }

    private void InvokeOnError(Exception ex)
    {
        try
        {
            OnError?.Invoke(ex);
        }
        catch (Exception handlerEx)
        {
            Debug.WriteLine($"[StreamHandler] Error in OnError handler: {handlerEx.Message}");
        }
    }

    private void FireComplete()
    {
        lock (_completeLock)
        {
            if (_completeFired) return;
            _completeFired = true;

            Debug.WriteLine("[StreamHandler] Firing OnComplete event");
            try
            {
                OnComplete?.Invoke();
                Debug.WriteLine("[StreamHandler] OnComplete event fired");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[StreamHandler] Error in OnComplete handler: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Stops the streaming
    /// </summary>
    public void StopStreaming()
    {
        if (!_disposed)
        {
            Debug.WriteLine("[StreamHandler] Stopping stream");
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
            Debug.WriteLine("[StreamHandler] Disposing resources");

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            _reader?.Dispose();
            _reader = null;

            _stream?.Dispose();
            _stream = null;

            _disposed = true;
        }
    }
}